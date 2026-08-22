from pathlib import Path
from tokenizers import Tokenizer
import onnxruntime as ort
import numpy as np

root = Path(__file__).resolve().parent

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

question = "می‌خوام پسورد حسابم رو عوض کنم."
faq = "چطور رمز عبورم را تغییر بدهم؟"

tokenizer = Tokenizer.from_file(
    str(tokenizer_path)
)

encoding = tokenizer.encode(
    question,
    faq
)

input_ids = np.asarray(
    [encoding.ids],
    dtype=np.int64
)

attention_mask = np.asarray(
    [encoding.attention_mask],
    dtype=np.int64
)

session = ort.InferenceSession(
    str(model_path),
    providers=[
        "CPUExecutionProvider"
    ]
)

outputs = session.run(
    None,
    {
        "input_ids": input_ids,
        "attention_mask": attention_mask
    }
)

score = float(
    np.asarray(outputs[0])
    .reshape(-1)[0]
)

print(
    f"Input length: {len(encoding.ids)}"
)

print(
    f"BGE Python score: {score:.9f}"
)