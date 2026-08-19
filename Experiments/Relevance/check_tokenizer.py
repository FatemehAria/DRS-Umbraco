from tokenizers import Tokenizer
from pathlib import Path

root = Path(__file__).resolve().parent

tokenizer_path = (
    root
    / "Model"
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

print("OFFICIAL IDS:")
print(encoding.ids)

print()
print("OFFICIAL TOKENS:")
print(encoding.tokens)

print()
print("ATTENTION MASK:")
print(encoding.attention_mask)

print()
print("LENGTH:")
print(len(encoding.ids))