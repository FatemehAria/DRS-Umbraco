from pathlib import Path
from tokenizers import Tokenizer
import onnxruntime as ort
import numpy as np
import csv
import json
import time

root = Path(__file__).resolve().parent
csv_path = root / "chatbot-relevance-verifier-devset.csv"
knowledge_path = root / "knowledge.json"
model_path = root / "Model-BGE" / "model.onnx"
tokenizer_path = root / "Model-BGE" / "tokenizer.json"

with knowledge_path.open("r", encoding="utf-8-sig") as file:
    knowledge = json.load(file)

answer_items = [item for item in knowledge if item["kind"] == 0]
knowledge_by_question = {item["question"]: item for item in answer_items}

print(f"Answer knowledge items: {len(answer_items)}")

rows = []
with csv_path.open("r", encoding="utf-8-sig", newline="") as file:
    reader = csv.DictReader(file)
    for row in reader:
        candidate_faq = row["CandidateFaq"]
        knowledge_item = knowledge_by_question.get(candidate_faq)

        if knowledge_item is None:
            raise RuntimeError(
                "Could not find knowledge item for CandidateFaq:\n"
                f"{candidate_faq}"
            )

        rows.append({
            "id": row["Id"],
            "label": row["Label"],
            "user_question": row["UserQuestion"],
            "knowledge_item": knowledge_item,
        })

print(f"Dev-set rows: {len(rows)}")
print("All CandidateFaq values matched knowledge.json successfully.")
print()

MODES = [
    "Question",
    "Question+Alternatives",
    "Full",
    "BestQuestionVariant",
]

def build_candidate_text(knowledge_item, mode):
    question = knowledge_item["question"]
    alternatives = knowledge_item.get("alternativeQuestions", [])
    answer = knowledge_item["answer"]

    if mode == "Question":
        return question

    if mode == "Question+Alternatives":
        return "\n".join([question, *alternatives])

    if mode == "Full":
        return "\n".join([question, *alternatives, answer])

    raise ValueError(f"Unknown text mode: {mode}")

def get_question_variants(knowledge_item):
    return [
        knowledge_item["question"],
        *knowledge_item.get("alternativeQuestions", []),
    ]

tokenizer = Tokenizer.from_file(str(tokenizer_path))
tokenizer.enable_truncation(
    max_length=512,
    strategy="longest_first",
    direction="right"
)

session = ort.InferenceSession(
    str(model_path),
    providers=["CPUExecutionProvider"]
)

input_names = {item.name for item in session.get_inputs()}

print("MODEL INPUTS:")
for item in session.get_inputs():
    print(f"- {item.name} | type={item.type} | shape={item.shape}")

print()
print("MODEL OUTPUTS:")
for item in session.get_outputs():
    print(f"- {item.name} | type={item.type} | shape={item.shape}")
print()

def score_pair(question, candidate_text):
    encoding = tokenizer.encode(question, candidate_text)

    input_ids = np.asarray([encoding.ids], dtype=np.int64)
    attention_mask = np.asarray([encoding.attention_mask], dtype=np.int64)

    feeds = {}

    if "input_ids" in input_names:
        feeds["input_ids"] = input_ids

    if "attention_mask" in input_names:
        feeds["attention_mask"] = attention_mask

    if "token_type_ids" in input_names:
        feeds["token_type_ids"] = np.zeros_like(input_ids, dtype=np.int64)

    unsupported_inputs = input_names - set(feeds.keys())

    if unsupported_inputs:
        raise RuntimeError(
            "Unsupported model inputs: "
            + ", ".join(sorted(unsupported_inputs))
        )

    outputs = session.run(None, feeds)

    return float(np.asarray(outputs[0]).reshape(-1)[0])

def score_candidate(user_question, knowledge_item, mode):
    if mode == "BestQuestionVariant":
        variants = get_question_variants(knowledge_item)

        best_score = None
        best_variant = None
        call_count = 0

        for variant in variants:
            score = score_pair(user_question, variant)
            call_count += 1

            if best_score is None or score > best_score:
                best_score = score
                best_variant = variant

        return best_score, best_variant, call_count

    candidate_text = build_candidate_text(knowledge_item, mode)
    score = score_pair(user_question, candidate_text)

    return score, candidate_text, 1

def find_best_threshold(results):
    scores = sorted(set(item["score"] for item in results))

    thresholds = [scores[0] - 1.0]

    for left, right in zip(scores, scores[1:]):
        thresholds.append((left + right) / 2.0)

    thresholds.append(scores[-1] + 1.0)

    best = None

    for threshold in thresholds:
        tp = tn = fp = fn = 0

        for item in results:
            predicted_positive = item["score"] >= threshold
            actual_positive = item["label"] == "Positive"

            if predicted_positive and actual_positive:
                tp += 1
            elif not predicted_positive and not actual_positive:
                tn += 1
            elif predicted_positive and not actual_positive:
                fp += 1
            else:
                fn += 1

        accuracy = (tp + tn) / len(results)

        candidate = {
            "threshold": threshold,
            "accuracy": accuracy,
            "tp": tp,
            "tn": tn,
            "fp": fp,
            "fn": fn,
        }

        if best is None or candidate["accuracy"] > best["accuracy"]:
            best = candidate

    return best

def print_misclassified_cases(results, threshold):
    print()
    print("MISCLASSIFIED CASES")
    print("------------------------------")

    found_any = False

    for item in results:
        predicted_positive = item["score"] >= threshold
        actual_positive = item["label"] == "Positive"

        if predicted_positive == actual_positive:
            continue

        found_any = True
        error_type = "FP" if predicted_positive else "FN"

        print()
        print(
            f'{error_type} | {item["id"]} | '
            f'Score={item["score"]:.6f}'
        )
        print("User:")
        print(item["user_question"])
        print("Candidate:")
        print(item["candidate_question"])

        if item["mode"] == "BestQuestionVariant":
            print("Best matched variant:")
            print(item["matched_variant"])

    if not found_any:
        print()
        print("None")

all_summaries = []

for mode in MODES:
    print()
    print("=" * 60)
    print(f"BGE-INT8 | {mode}")
    print("=" * 60)

    results = []
    total_model_calls = 0
    start = time.perf_counter()

    for row in rows:
        score, matched_variant, call_count = score_candidate(
            row["user_question"],
            row["knowledge_item"],
            mode
        )

        total_model_calls += call_count

        results.append({
            "id": row["id"],
            "label": row["label"],
            "score": score,
            "mode": mode,
            "user_question": row["user_question"],
            "candidate_question": row["knowledge_item"]["question"],
            "matched_variant": matched_variant,
        })

    elapsed = time.perf_counter() - start

    positive_scores = [
        item["score"]
        for item in results
        if item["label"] == "Positive"
    ]

    negative_scores = [
        item["score"]
        for item in results
        if item["label"] == "Negative"
    ]

    gap = min(positive_scores) - max(negative_scores)
    best = find_best_threshold(results)

    average_ms_per_row = elapsed * 1000 / len(results)
    average_ms_per_model_call = elapsed * 1000 / total_model_calls

    print(f"Positive Min: {min(positive_scores):.6f}")
    print(f"Positive Avg: {np.mean(positive_scores):.6f}")
    print(f"Negative Max: {max(negative_scores):.6f}")
    print(f"Negative Avg: {np.mean(negative_scores):.6f}")
    print(f"Gap: {gap:.6f}")
    print(f"Best threshold: {best['threshold']:.6f}")
    print(f"Best accuracy: {best['accuracy'] * 100:.1f}%")
    print(
        f"TP={best['tp']} TN={best['tn']} "
        f"FP={best['fp']} FN={best['fn']}"
    )
    print(f"Total model calls: {total_model_calls}")
    print(f"Average per dev row: {average_ms_per_row:.2f} ms")
    print(
        f"Average per model call: "
        f"{average_ms_per_model_call:.2f} ms"
    )

    if mode in ("Full", "BestQuestionVariant"):
        print_misclassified_cases(results, best["threshold"])

    all_summaries.append({
        "mode": mode,
        "gap": gap,
        "accuracy": best["accuracy"],
        "threshold": best["threshold"],
        "tp": best["tp"],
        "tn": best["tn"],
        "fp": best["fp"],
        "fn": best["fn"],
        "average_ms_per_row": average_ms_per_row,
        "average_ms_per_model_call": average_ms_per_model_call,
        "total_model_calls": total_model_calls,
    })

print()
print()
print("=" * 95)
print("FINAL COMPARISON")
print("=" * 95)

for item in sorted(
    all_summaries,
    key=lambda x: (x["accuracy"], x["gap"]),
    reverse=True
):
    print(
        f'{item["mode"]:<24} '
        f'Accuracy={item["accuracy"] * 100:>5.1f}%  '
        f'TP={item["tp"]:>2} '
        f'TN={item["tn"]:>2} '
        f'FP={item["fp"]:>2} '
        f'FN={item["fn"]:>2}  '
        f'Gap={item["gap"]:>9.4f}  '
        f'RowTime={item["average_ms_per_row"]:>8.2f}ms  '
        f'CallTime={item["average_ms_per_model_call"]:>8.2f}ms  '
        f'Calls={item["total_model_calls"]}'
    )
