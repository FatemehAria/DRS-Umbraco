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


MODELS = {
    "mMARCO-INT8": {
        "model": (
            root
            / "Model"
            / "model-int8-avx2.onnx"
        ),
        "tokenizer": (
            root
            / "Model"
            / "tokenizer.json"
        ),
    },

    "GTE-INT8": {
        "model": (
            root
            / "Model-GTE"
            / "model.onnx"
        ),
        "tokenizer": (
            root
            / "Model-GTE"
            / "tokenizer.json"
        ),
    },
}


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
# Candidate text modes
# ----------------------------------------

def build_candidate_text(
    knowledge_item,
    mode
):
    question = (
        knowledge_item["question"]
    )

    alternatives = (
        knowledge_item.get(
            "alternativeQuestions",
            []
        )
    )

    answer = (
        knowledge_item["answer"]
    )

    if mode == "Question":
        return question

    if mode == "Question+Alternatives":
        parts = [
            question,
            *alternatives
        ]

        return "\n".join(parts)

    if mode == "Full":
        parts = [
            question,
            *alternatives,
            answer
        ]

        return "\n".join(parts)

    raise ValueError(
        f"Unknown mode: {mode}"
    )


MODES = [
    "Question",
    "Question+Alternatives",
    "Full",
]


# ----------------------------------------
# Model runner
# ----------------------------------------

class Reranker:
    def __init__(
        self,
        model_path,
        tokenizer_path
    ):
        self.tokenizer = (
            Tokenizer.from_file(
                str(tokenizer_path)
            )
        )

        self.tokenizer.enable_truncation(
            max_length=512,
            strategy="longest_first",
            direction="right"
        )

        self.session = (
            ort.InferenceSession(
                str(model_path),
                providers=[
                    "CPUExecutionProvider"
                ]
            )
        )

        self.input_names = {
            item.name
            for item
            in self.session.get_inputs()
        }

    def score(
        self,
        question,
        candidate_text
    ):
        encoding = (
            self.tokenizer.encode(
                question,
                candidate_text
            )
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

        if "input_ids" in self.input_names:
            feeds["input_ids"] = (
                input_ids
            )

        if (
            "attention_mask"
            in self.input_names
        ):
            feeds["attention_mask"] = (
                attention_mask
            )

        if (
            "token_type_ids"
            in self.input_names
        ):
            feeds["token_type_ids"] = (
                np.zeros_like(
                    input_ids,
                    dtype=np.int64
                )
            )

        outputs = self.session.run(
            None,
            feeds
        )

        return float(
            np.asarray(outputs[0])
            .reshape(-1)[0]
        )


# ----------------------------------------
# Metrics
# ----------------------------------------

def find_best_threshold(results):
    scores = sorted(
        set(
            item["score"]
            for item in results
        )
    )

    thresholds = []

    thresholds.append(
        scores[0] - 1.0
    )

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
# Run experiments
# ----------------------------------------

all_summaries = []

for model_name, model_info in MODELS.items():

    print()
    print("=" * 60)
    print(model_name)
    print("=" * 60)

    runner = Reranker(
        model_info["model"],
        model_info["tokenizer"]
    )

    for mode in MODES:

        print()
        print(
            f"--- {mode} ---"
        )

        results = []

        start = time.perf_counter()

        for row in rows:
            candidate_text = (
                build_candidate_text(
                    row["knowledge_item"],
                    mode
                )
            )

            score = runner.score(
                row["user_question"],
                candidate_text
            )

            results.append(
                {
                    "id": row["id"],
                    "label": row["label"],
                    "score": score,
                    "user_question": row["user_question"],
                    "candidate_question":
                        row["knowledge_item"]["question"],
                }
            )

        elapsed = (
            time.perf_counter()
            - start
        )

        positive_scores = [
            item["score"]
            for item in results
            if item["label"]
            == "Positive"
        ]

        negative_scores = [
            item["score"]
            for item in results
            if item["label"]
            == "Negative"
        ]

        gap = (
            min(positive_scores)
            - max(negative_scores)
        )

        best = (
            find_best_threshold(
                results
            )
        )

        if (
            model_name == "mMARCO-INT8"
            and mode == "Full"
        ):
            print()
            print("MISCLASSIFIED CASES")
            print("------------------------------")

            for item in results:
                predicted_positive = (
                    item["score"]
                    >= best["threshold"]
                )

                actual_positive = (
                    item["label"]
                    == "Positive"
                )

                if predicted_positive != actual_positive:
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
                    print(item["user_question"])

                    print("Candidate:")
                    print(item["candidate_question"])
        average_ms = (
            elapsed
            * 1000
            / len(results)
        )

        print(
            f"Positive Min: "
            f"{min(positive_scores):.6f}"
        )

        print(
            f"Positive Avg: "
            f"{np.mean(positive_scores):.6f}"
        )

        print(
            f"Negative Max: "
            f"{max(negative_scores):.6f}"
        )

        print(
            f"Negative Avg: "
            f"{np.mean(negative_scores):.6f}"
        )

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
            f"Average per pair: "
            f"{average_ms:.2f} ms"
        )

        all_summaries.append(
            {
                "model": model_name,
                "mode": mode,
                "gap": gap,
                "accuracy": (
                    best["accuracy"]
                ),
                "average_ms": (
                    average_ms
                ),
            }
        )


# ----------------------------------------
# Final comparison
# ----------------------------------------

print()
print()
print("=" * 70)
print("FINAL COMPARISON")
print("=" * 70)

for item in sorted(
    all_summaries,
    key=lambda x: (
        x["accuracy"],
        x["gap"]
    ),
    reverse=True
):
    print(
        f'{item["model"]:<15} '
        f'{item["mode"]:<24} '
        f'Accuracy='
        f'{item["accuracy"] * 100:>5.1f}%  '
        f'Gap={item["gap"]:>9.4f}  '
        f'Time={item["average_ms"]:>7.2f}ms'
    )