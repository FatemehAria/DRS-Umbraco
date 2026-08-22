from pathlib import Path
from tokenizers import Tokenizer

root = Path(__file__).resolve().parent

tokenizer_path = (
    root
    / "Model-BGE"
    / "tokenizer.json"
)

tokenizer = Tokenizer.from_file(
    str(tokenizer_path)
)

question = "می‌خوام پسورد حسابم رو عوض کنم."
faq = "چطور رمز عبورم را تغییر بدهم؟"

encoding = tokenizer.encode(
    question,
    faq
)

print("OFFICIAL BGE IDS:")
print(encoding.ids)

print()
print("OFFICIAL BGE TOKENS:")
print(encoding.tokens)

print()
print("ATTENTION MASK:")
print(encoding.attention_mask)

print()
print("LENGTH:")
print(len(encoding.ids))