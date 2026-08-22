from pathlib import Path
import csv
import json

import numpy as np
from tokenizers import Tokenizer

from onnxruntime.quantization import (
    CalibrationDataReader,
    QuantFormat,
    QuantType,
    quantize_static,
)

from onnxruntime.quantization.calibrate import (
    CalibrationMethod,
)


ROOT = Path(__file__).resolve().parent

FP32_MODEL = (
    ROOT
    / "Model-BGE-FP32"
    / "onnx"
    / "model.onnx"
)

TOKENIZER_PATH = (
    ROOT
    / "Model-BGE"
    / "tokenizer.json"
)

DEVSET_PATH = (
    ROOT
    / "chatbot-relevance-verifier-devset.csv"
)

KNOWLEDGE_PATH = (
    ROOT
    / "knowledge.json"
)

OUTPUT_DIR = (
    ROOT
    / "Model-BGE-Static-INT8"
)

OUTPUT_MODEL = (
    OUTPUT_DIR
    / "model.onnx"
)

PAD_TOKEN_ID = 1
MAX_LENGTH = 512


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


def load_questions():
    questions = []

    with DEVSET_PATH.open(
        "r",
        encoding="utf-8-sig",
        newline=""
    ) as file:
        reader = csv.DictReader(file)

        for row in reader:
            question = row[
                "UserQuestion"
            ].strip()

            if (
                question
                and question not in questions
            ):
                questions.append(
                    question
                )

    return questions


def load_knowledge_items():
    with KNOWLEDGE_PATH.open(
        "r",
        encoding="utf-8-sig"
    ) as file:
        payload = json.load(file)

    if isinstance(payload, list):
        return payload

    if isinstance(payload, dict):
        for key in (
            "items",
            "knowledgeItems",
            "data"
        ):
            value = get_value(
                payload,
                key
            )

            if isinstance(value, list):
                return value

    raise ValueError(
        "Could not find knowledge items "
        "inside knowledge.json."
    )


def is_answer_item(item):
    kind = get_value(
        item,
        "kind"
    )

    if isinstance(kind, int):
        return kind == 0

    if isinstance(kind, str):
        return (
            kind.strip().lower()
            == "answer"
        )

    return False


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

    if isinstance(
        alternatives,
        str
    ):
        alternatives = [
            alternatives
        ]

    parts = [
        question,
        *alternatives,
        answer,
    ]

    parts = [
        str(part).strip()
        for part in parts
        if str(part).strip()
    ]

    # Deliberately use LF because
    # this matches the Python validation format.
    return "\n".join(parts)


def load_candidate_texts():
    items = load_knowledge_items()

    answer_items = [
        item
        for item in items
        if is_answer_item(item)
    ]

    candidate_texts = [
        build_candidate_text(item)
        for item in answer_items
    ]

    candidate_texts = [
        text
        for text in candidate_texts
        if text
    ]

    if len(candidate_texts) != 10:
        raise ValueError(
            "Expected exactly 10 Answer FAQs, "
            f"but found {len(candidate_texts)}."
        )

    return candidate_texts


def create_batch(
    tokenizer,
    question,
    candidate_texts
):
    encoded = [
        tokenizer.encode(
            question,
            candidate
        )
        for candidate
        in candidate_texts
    ]

    max_length = max(
        len(item.ids)
        for item in encoded
    )

    input_ids = np.full(
        (
            len(encoded),
            max_length
        ),
        PAD_TOKEN_ID,
        dtype=np.int64
    )

    attention_mask = np.zeros(
        (
            len(encoded),
            max_length
        ),
        dtype=np.int64
    )

    for index, item in enumerate(
        encoded
    ):
        length = len(item.ids)

        input_ids[
            index,
            :length
        ] = item.ids

        attention_mask[
            index,
            :length
        ] = item.attention_mask

    return {
        "input_ids":
            input_ids,

        "attention_mask":
            attention_mask,
    }


class BgeCalibrationDataReader(
    CalibrationDataReader
):
    def __init__(self, batches):
        self._batches = batches
        self.rewind()

    def get_next(self):
        return next(
            self._iterator,
            None
        )

    def rewind(self):
        self._iterator = iter(
            self._batches
        )


def main():
    print(
        "Loading tokenizer..."
    )

    tokenizer = Tokenizer.from_file(
        str(TOKENIZER_PATH)
    )

    tokenizer.enable_truncation(
        max_length=MAX_LENGTH,
        strategy="longest_first"
    )

    questions = load_questions()
    candidate_texts = (
        load_candidate_texts()
    )

    print(
        f"Dev questions: "
        f"{len(questions)}"
    )

    print(
        f"Answer FAQs: "
        f"{len(candidate_texts)}"
    )

    batches = [
        create_batch(
            tokenizer,
            question,
            candidate_texts
        )
        for question in questions
    ]

    total_pairs = (
        len(questions)
        * len(candidate_texts)
    )

    print(
        f"Calibration batches: "
        f"{len(batches)}"
    )

    print(
        f"Calibration pairs: "
        f"{total_pairs}"
    )

    OUTPUT_DIR.mkdir(
        parents=True,
        exist_ok=True
    )

    reader = (
        BgeCalibrationDataReader(
            batches
        )
    )

    print()
    print(
        "Starting static "
        "INT8 quantization..."
    )

    quantize_static(
        model_input=
            str(FP32_MODEL),

        model_output=
            str(OUTPUT_MODEL),

        calibration_data_reader=
            reader,

        quant_format=
            QuantFormat.QDQ,

        activation_type=
            QuantType.QInt8,

        weight_type=
            QuantType.QInt8,

        per_channel=True,

        reduce_range=False,

        op_types_to_quantize=[
            "MatMul",
            "Gemm",
        ],

        calibrate_method=
            CalibrationMethod.MinMax,

        use_external_data_format=True,
    )

    print()
    print(
        "Quantization completed."
    )

    print(
        f"Output: "
        f"{OUTPUT_MODEL}"
    )


if __name__ == "__main__":
    main()