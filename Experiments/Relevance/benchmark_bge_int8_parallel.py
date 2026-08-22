from pathlib import Path
from concurrent.futures import ThreadPoolExecutor
import json
import time

import numpy as np
import onnxruntime as ort
from tokenizers import Tokenizer


ROOT = Path(__file__).resolve().parent

MODEL_PATH = (
    ROOT
    / "Model-BGE"
    / "model.onnx"
)

TOKENIZER_PATH = (
    ROOT
    / "Model-BGE"
    / "tokenizer.json"
)

KNOWLEDGE_PATH = (
    ROOT
    / "knowledge.json"
)

QUESTION = (
    "می‌خوام آدرس ایمیل متصل "
    "به حسابم رو عوض کنم."
)


def get_value(obj, name):
    for key, value in obj.items():
        if key.lower() == name.lower():
            return value

    return None


def build_candidate_text(item):
    question = (
        get_value(item, "question")
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
        get_value(item, "answer")
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


def load_candidates():
    with KNOWLEDGE_PATH.open(
        "r",
        encoding="utf-8-sig"
    ) as file:
        payload = json.load(file)

    if isinstance(payload, list):
        items = payload
    else:
        items = (
            payload.get("items")
            or payload.get(
                "knowledgeItems"
            )
            or payload.get("data")
            or []
        )

    candidates = []

    for item in items:
        kind = get_value(
            item,
            "kind"
        )

        is_answer = (
            kind == 0
            or (
                isinstance(kind, str)
                and kind.lower()
                == "answer"
            )
        )

        if is_answer:
            candidates.append(
                build_candidate_text(
                    item
                )
            )

    return candidates


tokenizer = Tokenizer.from_file(
    str(TOKENIZER_PATH)
)

candidates = load_candidates()

encoded_inputs = []

for candidate in candidates:
    encoding = tokenizer.encode(
        QUESTION,
        candidate
    )

    encoded_inputs.append(
        (
            np.asarray(
                [encoding.ids],
                dtype=np.int64
            ),
            np.asarray(
                [encoding.attention_mask],
                dtype=np.int64
            )
        )
    )


def create_session(
    intra_threads=None
):
    options = ort.SessionOptions()

    options.inter_op_num_threads = 1

    if intra_threads is not None:
        options.intra_op_num_threads = (
            intra_threads
        )

    options.execution_mode = (
        ort.ExecutionMode.ORT_SEQUENTIAL
    )

    return ort.InferenceSession(
        str(MODEL_PATH),
        sess_options=options,
        providers=[
            "CPUExecutionProvider"
        ]
    )


def run_one(
    session,
    model_input
):
    input_ids, attention_mask = (
        model_input
    )

    output = session.run(
        None,
        {
            "input_ids":
                input_ids,

            "attention_mask":
                attention_mask
        }
    )

    return float(
        np.asarray(output[0])
        .reshape(-1)[0]
    )


def benchmark(
    name,
    workers,
    intra_threads
):
    session = create_session(
        intra_threads
    )

    # Reference scores
    reference_scores = [
        run_one(session, item)
        for item in encoded_inputs
    ]

    # Warm-up
    for _ in range(2):
        with ThreadPoolExecutor(
            max_workers=workers
        ) as executor:
            list(
                executor.map(
                    lambda item:
                        run_one(
                            session,
                            item
                        ),
                    encoded_inputs
                )
            )

    times = []
    parallel_scores = None

    for _ in range(10):
        start = time.perf_counter()

        with ThreadPoolExecutor(
            max_workers=workers
        ) as executor:
            parallel_scores = list(
                executor.map(
                    lambda item:
                        run_one(
                            session,
                            item
                        ),
                    encoded_inputs
                )
            )

        elapsed_ms = (
            time.perf_counter()
            - start
        ) * 1000

        times.append(elapsed_ms)

    max_diff = max(
        abs(a - b)
        for a, b in zip(
            reference_scores,
            parallel_scores
        )
    )

    print()
    print(name)
    print("=" * len(name))

    print(
        f"Workers: "
        f"{workers}"
    )

    print(
        f"Intra-op threads: "
        f"{intra_threads}"
    )

    print(
        f"Average: "
        f"{sum(times) / len(times):.2f} ms"
    )

    print(
        f"Min:     "
        f"{min(times):.2f} ms"
    )

    print(
        f"Max:     "
        f"{max(times):.2f} ms"
    )

    print(
        f"Max score diff: "
        f"{max_diff:.9f}"
    )


print(
    f"Full candidates: "
    f"{len(candidates)}"
)


benchmark(
    "Sequential - 1 worker / 1 thread",
    workers=1,
    intra_threads=1
)

benchmark(
    "Parallel - 2 workers / 1 thread",
    workers=2,
    intra_threads=1
)

benchmark(
    "Parallel - 4 workers / 1 thread",
    workers=4,
    intra_threads=1
)

benchmark(
    "Parallel - 2 workers / 2 threads",
    workers=2,
    intra_threads=2
)