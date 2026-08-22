from pathlib import Path
import time

import numpy as np
import onnxruntime as ort
from tokenizers import Tokenizer
import json


root = Path(__file__).resolve().parent

model_path = (
    root
    / "Model-BGE-FP32"
    / "onnx"
    / "model.onnx"
)

tokenizer_path = (
    root
    / "Model-BGE"
    / "tokenizer.json"
)

tokenizer = Tokenizer.from_file(
    str(tokenizer_path)
)

session = ort.InferenceSession(
    str(model_path),
    providers=["CPUExecutionProvider"]
)


question = "می‌خوام آدرس ایمیل متصل به حسابم رو عوض کنم."

knowledge_path = (
    root
    / "knowledge.json"
)


def get_value(obj, *names):
    lower_map = {
        str(key).lower(): value
        for key, value in obj.items()
    }

    for name in names:
        key = name.lower()

        if key in lower_map:
            return lower_map[key]

    return None


def build_candidate_text(item):
    question = (
        get_value(
            item,
            "question"
        )
        or ""
    )

    alternatives = (
        get_value(
            item,
            "alternativeQuestions"
        )
        or []
    )

    answer = (
        get_value(
            item,
            "answer"
        )
        or ""
    )

    if isinstance(alternatives, str):
        alternatives = [alternatives]

    parts = [
        question,
        *alternatives,
        answer
    ]

    return "\n".join(
        str(part).strip()
        for part in parts
        if str(part).strip()
    )


with knowledge_path.open(
    "r",
    encoding="utf-8-sig"
) as file:
    payload = json.load(file)


if isinstance(payload, list):
    items = payload
else:
    items = (
        payload.get("items")
        or payload.get("knowledgeItems")
        or payload.get("data")
        or []
    )


answer_items = []

for item in items:
    kind = get_value(
        item,
        "kind"
    )

    is_answer = (
        kind == 0
        or (
            isinstance(kind, str)
            and kind.lower() == "answer"
        )
    )

    if is_answer:
        answer_items.append(item)


candidates = [
    build_candidate_text(item)
    for item in answer_items
]


print(
    f"Full Answer candidates: "
    f"{len(candidates)}"
)


def encode_pair(candidate):
    encoding = tokenizer.encode(
        question,
        candidate
    )

    return (
        encoding.ids,
        encoding.attention_mask
    )


encoded = [
    encode_pair(candidate)
    for candidate in candidates
]


def run_individually():
    scores = []

    for ids, mask in encoded:
        input_ids = np.asarray(
            [ids],
            dtype=np.int64
        )

        attention_mask = np.asarray(
            [mask],
            dtype=np.int64
        )

        output = session.run(
            None,
            {
                "input_ids": input_ids,
                "attention_mask": attention_mask
            }
        )

        scores.append(
            float(
                np.asarray(output[0])
                .reshape(-1)[0]
            )
        )

    return scores


def run_batch():
    max_length = max(
        len(ids)
        for ids, _ in encoded
    )

    input_ids = np.full(
        (len(encoded), max_length),
        1,
        dtype=np.int64
    )

    attention_mask = np.zeros(
        (len(encoded), max_length),
        dtype=np.int64
    )

    for index, (ids, mask) in enumerate(encoded):
        length = len(ids)

        input_ids[
            index,
            :length
        ] = ids

        attention_mask[
            index,
            :length
        ] = mask

    output = session.run(
        None,
        {
            "input_ids": input_ids,
            "attention_mask": attention_mask
        }
    )

    return (
        np.asarray(output[0])
        .reshape(-1)
        .tolist()
    )


# Warm-up
for _ in range(3):
    run_individually()
    run_batch()


individual_times = []
batch_times = []

runs = 10

for _ in range(runs):
    start = time.perf_counter()
    individual_scores = run_individually()
    individual_times.append(
        (time.perf_counter() - start) * 1000
    )

    start = time.perf_counter()
    batch_scores = run_batch()
    batch_times.append(
        (time.perf_counter() - start) * 1000
    )


print()
print("SCORE PARITY")
print("============")

for i, (
    individual_score,
    batch_score
) in enumerate(
    zip(
        individual_scores,
        batch_scores
    )
):
    print(
        f"{i}: "
        f"single={individual_score:.9f} "
        f"batch={batch_score:.9f} "
        f"diff={batch_score - individual_score:.9f}"
    )


print()
print("PERFORMANCE")
print("===========")

print(
    "10 individual runs average: "
    f"{sum(individual_times) / len(individual_times):.2f} ms"
)

print(
    "1 batch of 10 average:      "
    f"{sum(batch_times) / len(batch_times):.2f} ms"
)

print(
    "Batch min:                  "
    f"{min(batch_times):.2f} ms"
)

print(
    "Batch max:                  "
    f"{max(batch_times):.2f} ms"
)