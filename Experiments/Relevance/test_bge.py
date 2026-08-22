from pathlib import Path
from tokenizers import Tokenizer
import onnxruntime as ort
import numpy as np
import csv
import json
import time


root = Path(__file__).resolve().parent

csv_path = (
    root
    / "chatbot-relevance-verifier-devset.csv"
)

knowledge_path = (
    root
    / "knowledge.json"
)

model_path = (
    root
    / "Model-BGE"
    / "model.onnx"
)

tokenizer_path = (
    root
    / "Model-BGE"
    / "tokenizer.json"
)


# ----------------------------------------
# Load knowledge
# ----------------------------------------

with knowledge_path.open(
    "r",
    encoding="utf-8-sig"
) as file:
    knowledge = json.load(file)


answer_items = [
    item
    for item in knowledge
    if item["kind"] == 0
]


knowledge_by_question = {
    item["question"]: item
    for item in answer_items
}


print(
    f"Answer knowledge items: "
    f"{len(answer_items)}"
)


# ----------------------------------------
# Load dev set
# ----------------------------------------

rows = []

with csv_path.open(
    "r",
    encoding="utf-8-sig",
    newline=""
) as file:

    reader = csv.DictReader(file)

    for row in reader:
        candidate_faq = row["CandidateFaq"]

        knowledge_item = (
            knowledge_by_question.get(
                candidate_faq
            )
        )

        if knowledge_item is None:
            raise RuntimeError(
                "Could not find knowledge item "
                f"for CandidateFaq:\n"
                f"{candidate_faq}"
            )

        rows.append(
            {
                "id": row["Id"],
                "label": row["Label"],
                "user_question": (
                    row["UserQuestion"]
                ),
                "knowledge_item": (
                    knowledge_item
                ),
            }
        )


print(
    f"Dev-set rows: {len(rows)}"
)

print(
    "All CandidateFaq values matched "
    "knowledge.json successfully."
)

print()


# ----------------------------------------
# Build the Full candidate text
# ----------------------------------------

def build_full_candidate_text(
    knowledge_item
):
    return "\n".join(
        [
            knowledge_item["question"],
            *knowledge_item.get(
                "alternativeQuestions",
                []
            ),
            knowledge_item["answer"],
        ]
    )


# ----------------------------------------
# Load tokenizer and ONNX model
# ----------------------------------------

tokenizer = Tokenizer.from_file(
    str(tokenizer_path)
)

tokenizer.enable_truncation(
    max_length=512,
    strategy="longest_first",
    direction="right"
)

session = ort.InferenceSession(
    str(model_path),
    providers=[
        "CPUExecutionProvider"
    ]
)

input_names = {
    item.name
    for item in session.get_inputs()
}


print("MODEL INPUTS:")

for item in session.get_inputs():
    print(
        f"- {item.name} | "
        f"type={item.type} | "
        f"shape={item.shape}"
    )


print()
print("MODEL OUTPUTS:")

for item in session.get_outputs():
    print(
        f"- {item.name} | "
        f"type={item.type} | "
        f"shape={item.shape}"
    )

print()


# ----------------------------------------
# Score one query/candidate pair
# ----------------------------------------

def score_pair(
    question,
    candidate_text
):
    encoding = tokenizer.encode(
        question,
        candidate_text
    )

    input_ids = np.asarray(
        [encoding.ids],
        dtype=np.int64
    )

    attention_mask = np.asarray(
        [encoding.attention_mask],
        dtype=np.int64
    )

    feeds = {}

    if "input_ids" in input_names:
        feeds["input_ids"] = (
            input_ids
        )

    if "attention_mask" in input_names:
        feeds["attention_mask"] = (
            attention_mask
        )

    if "token_type_ids" in input_names:
        feeds["token_type_ids"] = (
            np.zeros_like(
                input_ids,
                dtype=np.int64
            )
        )

    unsupported_inputs = (
        input_names
        - set(feeds.keys())
    )

    if unsupported_inputs:
        raise RuntimeError(
            "Unsupported model inputs: "
            + ", ".join(
                sorted(
                    unsupported_inputs
                )
            )
        )

    outputs = session.run(
        None,
        feeds
    )

    return float(
        np.asarray(outputs[0])
        .reshape(-1)[0]
    )


# ----------------------------------------
# Find best experimental threshold
# ----------------------------------------

def find_best_threshold(results):
    scores = sorted(
        set(
            item["score"]
            for item in results
        )
    )

    thresholds = [
        scores[0] - 1.0
    ]

    for left, right in zip(
        scores,
        scores[1:]
    ):
        thresholds.append(
            (left + right) / 2.0
        )

    thresholds.append(
        scores[-1] + 1.0
    )

    best = None

    for threshold in thresholds:
        tp = 0
        tn = 0
        fp = 0
        fn = 0

        for item in results:
            predicted_positive = (
                item["score"]
                >= threshold
            )

            actual_positive = (
                item["label"]
                == "Positive"
            )

            if (
                predicted_positive
                and actual_positive
            ):
                tp += 1

            elif (
                not predicted_positive
                and not actual_positive
            ):
                tn += 1

            elif (
                predicted_positive
                and not actual_positive
            ):
                fp += 1

            else:
                fn += 1

        accuracy = (
            (tp + tn)
            / len(results)
        )

        candidate = {
            "threshold": threshold,
            "accuracy": accuracy,
            "tp": tp,
            "tn": tn,
            "fp": fp,
            "fn": fn,
        }

        if (
            best is None
            or candidate["accuracy"]
            > best["accuracy"]
        ):
            best = candidate

    return best


# ----------------------------------------
# Run BGE on the 30-row dev set
# ----------------------------------------

results = []

start = time.perf_counter()

for row in rows:
    candidate_text = (
        build_full_candidate_text(
            row["knowledge_item"]
        )
    )

    score = score_pair(
        row["user_question"],
        candidate_text
    )

    results.append(
        {
            "id": row["id"],
            "label": row["label"],
            "score": score,
            "user_question": (
                row["user_question"]
            ),
            "candidate_question": (
                row["knowledge_item"]
                ["question"]
            ),
        }
    )

    print(
        f'{row["id"]:<4} '
        f'{row["label"]:<8} '
        f'{score:>10.6f}'
    )


elapsed = (
    time.perf_counter()
    - start
)


# ----------------------------------------
# Summary
# ----------------------------------------

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

gap = (
    min(positive_scores)
    - max(negative_scores)
)

best = find_best_threshold(
    results
)

average_ms = (
    elapsed
    * 1000
    / len(results)
)


print()
print("==============================")
print("SUMMARY")
print("==============================")
print()

print("Positive:")
print(
    f"  Min: "
    f"{min(positive_scores):.6f}"
)
print(
    f"  Max: "
    f"{max(positive_scores):.6f}"
)
print(
    f"  Avg: "
    f"{np.mean(positive_scores):.6f}"
)

print()
print("Negative:")
print(
    f"  Min: "
    f"{min(negative_scores):.6f}"
)
print(
    f"  Max: "
    f"{max(negative_scores):.6f}"
)
print(
    f"  Avg: "
    f"{np.mean(negative_scores):.6f}"
)

print()
print(
    f"Gap: {gap:.6f}"
)

print(
    f"Best threshold: "
    f"{best['threshold']:.6f}"
)

print(
    f"Best accuracy: "
    f"{best['accuracy'] * 100:.1f}%"
)

print(
    f"TP={best['tp']} "
    f"TN={best['tn']} "
    f"FP={best['fp']} "
    f"FN={best['fn']}"
)

print(
    f"Total time: "
    f"{elapsed * 1000:.0f} ms"
)

print(
    f"Average per pair: "
    f"{average_ms:.2f} ms"
)


# ----------------------------------------
# Misclassified cases
# ----------------------------------------

print()
print("MISCLASSIFIED CASES")
print("------------------------------")

found_any = False

for item in results:
    predicted_positive = (
        item["score"]
        >= best["threshold"]
    )

    actual_positive = (
        item["label"]
        == "Positive"
    )

    if predicted_positive == actual_positive:
        continue

    found_any = True

    error_type = (
        "FP"
        if predicted_positive
        else "FN"
    )

    print()
    print(
        f'{error_type} | '
        f'{item["id"]} | '
        f'Score={item["score"]:.6f}'
    )

    print("User:")
    print(
        item["user_question"]
    )

    print("Candidate:")
    print(
        item["candidate_question"]
    )

if not found_any:
    print()
    print("None")


# ----------------------------------------
# Sorted scores
# ----------------------------------------

print()
print("Scores from highest to lowest:")

for item in sorted(
    results,
    key=lambda x: x["score"],
    reverse=True
):
    print(
        f'{item["id"]:<4} '
        f'{item["label"]:<8} '
        f'{item["score"]:>10.6f}'
    )
