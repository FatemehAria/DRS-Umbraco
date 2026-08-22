from pathlib import Path
from tokenizers import Tokenizer
import onnxruntime as ort
import numpy as np
import csv
import time

root = Path(__file__).resolve().parent

model_path = root / "Model-GTE" / "model.onnx"
tokenizer_path = root / "Model-GTE" / "tokenizer.json"
csv_path = root / "chatbot-relevance-verifier-devset.csv"

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
    providers=["CPUExecutionProvider"]
)

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


def score_pair(question: str, candidate_faq: str) -> float:
    encoding = tokenizer.encode(
        question,
        candidate_faq
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

    for input_meta in session.get_inputs():
        if input_meta.name == "input_ids":
            feeds[input_meta.name] = input_ids

        elif input_meta.name == "attention_mask":
            feeds[input_meta.name] = attention_mask

        elif input_meta.name == "token_type_ids":
            feeds[input_meta.name] = np.zeros_like(
                input_ids,
                dtype=np.int64
            )

        else:
            raise RuntimeError(
                f"Unsupported model input: "
                f"{input_meta.name}"
            )

    outputs = session.run(
        None,
        feeds
    )

    logits = outputs[0]

    return float(
        np.asarray(logits)
        .reshape(-1)[0]
    )


results = []

start = time.perf_counter()

with csv_path.open(
    "r",
    encoding="utf-8-sig",
    newline=""
) as file:

    reader = csv.DictReader(file)

    for row in reader:
        score = score_pair(
            row["UserQuestion"],
            row["CandidateFaq"]
        )

        results.append(
            {
                "id": row["Id"],
                "label": row["Label"],
                "score": score
            }
        )

        print(
            f'{row["Id"]:<4} '
            f'{row["Label"]:<8} '
            f'{score:>10.6f}'
        )

elapsed = time.perf_counter() - start

positive_scores = [
    x["score"]
    for x in results
    if x["label"] == "Positive"
]

negative_scores = [
    x["score"]
    for x in results
    if x["label"] == "Negative"
]

print()
print("==============================")
print("SUMMARY")
print("==============================")
print()

print("Positive:")
print(f"  Min: {min(positive_scores):.6f}")
print(f"  Max: {max(positive_scores):.6f}")
print(
    f"  Avg: "
    f"{sum(positive_scores) / len(positive_scores):.6f}"
)

print()
print("Negative:")
print(f"  Min: {min(negative_scores):.6f}")
print(f"  Max: {max(negative_scores):.6f}")
print(
    f"  Avg: "
    f"{sum(negative_scores) / len(negative_scores):.6f}"
)

gap = (
    min(positive_scores)
    - max(negative_scores)
)

print()
print(f"Gap: {gap:.6f}")

print()
print(
    f"Total time: "
    f"{elapsed * 1000:.0f} ms"
)

print(
    f"Average per pair: "
    f"{elapsed * 1000 / len(results):.2f} ms"
)

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