from pathlib import Path

import numpy as np
import onnxruntime as ort
from tokenizers import Tokenizer


root = Path(__file__).resolve().parent

model_path = (
    root
    / "Model-BGE-Static-INT8"
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


pairs = [
    (
        "می‌خوام پسورد حسابم رو عوض کنم.",
        "چطور رمز عبورم را تغییر بدهم؟"
    ),
    (
        "می‌خوام از حساب فعلی خارج بشم.",
        "چطور حساب کاربری‌ام را حذف کنم؟"
    )
]


def run_single(question, candidate):
    encoding = tokenizer.encode(
        question,
        candidate
    )

    input_ids = np.asarray(
        [encoding.ids],
        dtype=np.int64
    )

    attention_mask = np.asarray(
        [encoding.attention_mask],
        dtype=np.int64
    )

    output = session.run(
        None,
        {
            "input_ids": input_ids,
            "attention_mask": attention_mask
        }
    )

    score = float(
        np.asarray(output[0])
        .reshape(-1)[0]
    )

    return score, encoding.ids


single_results = []

for question, candidate in pairs:
    score, ids = run_single(
        question,
        candidate
    )

    single_results.append(
        (score, ids)
    )


max_length = max(
    len(ids)
    for _, ids in single_results
)

batch_size = len(single_results)

input_ids = np.full(
    (batch_size, max_length),
    1,
    dtype=np.int64
)

attention_mask = np.zeros(
    (batch_size, max_length),
    dtype=np.int64
)


for index, (_, ids) in enumerate(
    single_results
):
    length = len(ids)

    input_ids[
        index,
        :length
    ] = ids

    attention_mask[
        index,
        :length
    ] = 1


batch_output = session.run(
    None,
    {
        "input_ids": input_ids,
        "attention_mask": attention_mask
    }
)

batch_scores = (
    np.asarray(batch_output[0])
    .reshape(-1)
)


print("INPUT LENGTHS:")
for index, (_, ids) in enumerate(
    single_results
):
    print(
        f"{index}: {len(ids)}"
    )


print()
print(
    f"BATCH SHAPE: "
    f"{input_ids.shape}"
)


print()
print("SCORES:")

for index, (
    single_result,
    batch_score
) in enumerate(
    zip(
        single_results,
        batch_scores
    )
):
    single_score = (
        single_result[0]
    )

    print(
        f"{index}: "
        f"single={single_score:.9f} "
        f"batch={float(batch_score):.9f} "
        f"diff={float(batch_score) - single_score:.9f}"
    )


print()
print("INPUT IDS:")

print(input_ids)

print()
print("ATTENTION MASK:")

print(attention_mask)

print()
print("===================================")
print("TEST 1 - DUPLICATE SAME INPUT")
print("===================================")

first_ids = single_results[0][1]

duplicate_input_ids = np.asarray(
    [
        first_ids,
        first_ids
    ],
    dtype=np.int64
)

duplicate_attention_mask = np.ones(
    duplicate_input_ids.shape,
    dtype=np.int64
)

duplicate_output = session.run(
    None,
    {
        "input_ids": duplicate_input_ids,
        "attention_mask": duplicate_attention_mask
    }
)

duplicate_scores = (
    np.asarray(duplicate_output[0])
    .reshape(-1)
)

print(
    f"single first = "
    f"{single_results[0][0]:.9f}"
)

print(
    f"batch copy 1 = "
    f"{float(duplicate_scores[0]):.9f}"
)

print(
    f"batch copy 2 = "
    f"{float(duplicate_scores[1]):.9f}"
)


print()
print("===================================")
print("TEST 2 - PADDED INPUT RUN ALONE")
print("===================================")

second_ids = single_results[1][1]

padded_second_ids = np.full(
    (1, max_length),
    1,
    dtype=np.int64
)

padded_second_mask = np.zeros(
    (1, max_length),
    dtype=np.int64
)

padded_second_ids[
    0,
    :len(second_ids)
] = second_ids

padded_second_mask[
    0,
    :len(second_ids)
] = 1

padded_second_output = session.run(
    None,
    {
        "input_ids": padded_second_ids,
        "attention_mask": padded_second_mask
    }
)

padded_second_score = float(
    np.asarray(padded_second_output[0])
    .reshape(-1)[0]
)

print(
    f"second original single = "
    f"{single_results[1][0]:.9f}"
)

print(
    f"second padded single   = "
    f"{padded_second_score:.9f}"
)

print(
    f"second inside batch    = "
    f"{float(batch_scores[1]):.9f}"
)