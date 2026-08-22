from pathlib import Path
from huggingface_hub import hf_hub_download

repo_id = "onnx-community/bge-reranker-v2-m3-ONNX"

target = (
    Path(__file__).resolve().parent
    / "Model-BGE-FP32"
)

target.mkdir(
    parents=True,
    exist_ok=True
)

files = [
    "onnx/model.onnx",
    "onnx/model.onnx_data",
]

for filename in files:
    print(f"Downloading: {filename}")

    path = hf_hub_download(
        repo_id=repo_id,
        filename=filename,
        local_dir=str(target)
    )

    print(f"Saved: {path}")

print("Done.")