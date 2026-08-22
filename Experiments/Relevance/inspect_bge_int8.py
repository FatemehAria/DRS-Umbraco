from pathlib import Path
from collections import Counter

import onnx


root = Path(__file__).resolve().parent

model_path = (
    root
    / "Model-BGE"
    / "model.onnx"
)

model = onnx.load(
    str(model_path),
    load_external_data=False
)

operator_counts = Counter(
    node.op_type
    for node in model.graph.node
)

interesting = [
    "DynamicQuantizeLinear",
    "QuantizeLinear",
    "DequantizeLinear",
    "MatMulInteger",
    "QLinearMatMul",
    "MatMul"
]

print("QUANTIZATION OPERATORS")
print("======================")

for op in interesting:
    print(
        f"{op:24} "
        f"{operator_counts.get(op, 0)}"
    )

print()
print(
    "Total nodes:",
    len(model.graph.node)
)