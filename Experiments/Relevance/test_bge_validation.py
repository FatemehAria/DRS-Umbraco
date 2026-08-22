from pathlib import Path
from tokenizers import Tokenizer
import onnxruntime as ort
import numpy as np
import csv
import json
import time

root = Path(__file__).resolve().parent
csv_path = root / "chatbot-relevance-verifier-validation.csv"
knowledge_path = root / "knowledge.json"
model_path = root / "Model-BGE" / "model.onnx"
tokenizer_path = root / "Model-BGE" / "tokenizer.json"

# IMPORTANT:
# This threshold is frozen from the previous 30-row development set.
# Do NOT optimize a new threshold on this validation set.
THRESHOLD = -2.777814

with knowledge_path.open("r", encoding="utf-8-sig") as file:
    knowledge = json.load(file)

answer_items = [item for item in knowledge if item["kind"] == 0]
knowledge_by_question = {item["question"]: item for item in answer_items}

rows = []
with csv_path.open("r", encoding="utf-8-sig", newline="") as file:
    reader = csv.DictReader(file)
    for row in reader:
        knowledge_item = knowledge_by_question.get(row["CandidateFaq"])

        if knowledge_item is None:
            raise RuntimeError(
                "Could not find knowledge item for CandidateFaq:\n"
                + row["CandidateFaq"]
            )

        rows.append({
            "id": row["Id"],
            "label": row["Label"],
            "user_question": row["UserQuestion"],
            "candidate_intent": row["CandidateIntent"],
            "knowledge_item": knowledge_item,
        })

print(f"Validation rows: {len(rows)}")
print(f"Frozen threshold: {THRESHOLD:.6f}")
print()

def build_full_candidate_text(item):
    return "\n".join([
        item["question"],
        *item.get("alternativeQuestions", []),
        item["answer"],
    ])

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
        feeds["token_type_ids"] = np.zeros_like(
            input_ids,
            dtype=np.int64
        )

    unsupported = input_names - set(feeds.keys())
    if unsupported:
        raise RuntimeError(
            "Unsupported model inputs: "
            + ", ".join(sorted(unsupported))
        )

    outputs = session.run(None, feeds)
    return float(np.asarray(outputs[0]).reshape(-1)[0])

# Warm up once so timing reflects steady-state inference more closely.
warm_item = rows[0]
score_pair(
    warm_item["user_question"],
    build_full_candidate_text(warm_item["knowledge_item"])
)

results = []
start = time.perf_counter()

for row in rows:
    score = score_pair(
        row["user_question"],
        build_full_candidate_text(row["knowledge_item"])
    )

    predicted_positive = score >= THRESHOLD
    actual_positive = row["label"] == "Positive"

    if predicted_positive and actual_positive:
        outcome = "TP"
    elif (not predicted_positive) and (not actual_positive):
        outcome = "TN"
    elif predicted_positive and (not actual_positive):
        outcome = "FP"
    else:
        outcome = "FN"

    results.append({
        **row,
        "score": score,
        "outcome": outcome,
    })

    print(
        f'{row["id"]:<5} '
        f'{row["label"]:<8} '
        f'{score:>10.6f} '
        f'{outcome}'
    )

elapsed = time.perf_counter() - start

tp = sum(1 for x in results if x["outcome"] == "TP")
tn = sum(1 for x in results if x["outcome"] == "TN")
fp = sum(1 for x in results if x["outcome"] == "FP")
fn = sum(1 for x in results if x["outcome"] == "FN")

accuracy = (tp + tn) / len(results)

positive_scores = [
    x["score"] for x in results
    if x["label"] == "Positive"
]
negative_scores = [
    x["score"] for x in results
    if x["label"] == "Negative"
]

print()
print("=" * 55)
print("FIXED-THRESHOLD VALIDATION SUMMARY")
print("=" * 55)
print(f"Threshold: {THRESHOLD:.6f}")
print(f"Accuracy: {accuracy * 100:.1f}% ({tp + tn}/{len(results)})")
print(f"TP={tp} TN={tn} FP={fp} FN={fn}")
print(f"Positive recall: {tp / (tp + fn) * 100:.1f}%")
print(f"Negative rejection: {tn / (tn + fp) * 100:.1f}%")
print(f"Positive Min: {min(positive_scores):.6f}")
print(f"Negative Max: {max(negative_scores):.6f}")
print(
    f"Observed gap (diagnostic only): "
    f"{min(positive_scores) - max(negative_scores):.6f}"
)
print(
    f"Average per pair after warm-up: "
    f"{elapsed * 1000 / len(results):.2f} ms"
)

print()
print("PREDECLARED PASS CRITERIA")
print("-------------------------")
print("Accuracy >= 90.0%")
print("False positives <= 1")
print("Positive recall >= 85.0%")

passed = (
    accuracy >= 0.90
    and fp <= 1
    and (tp / (tp + fn)) >= 0.85
)

print()
print("RESULT:", "PASS" if passed else "FAIL")

print()
print("MISCLASSIFIED CASES")
print("-------------------")

errors = [x for x in results if x["outcome"] in ("FP", "FN")]

if not errors:
    print("None")
else:
    for item in errors:
        print()
        print(
            f'{item["outcome"]} | {item["id"]} | '
            f'Score={item["score"]:.6f} | '
            f'Candidate={item["candidate_intent"]}'
        )
        print("User:")
        print(item["user_question"])
        print("Candidate FAQ:")
        print(item["knowledge_item"]["question"])
