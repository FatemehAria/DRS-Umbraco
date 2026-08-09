from transformers import AutoTokenizer

text = "query: چطور رمز عبورم را تغییر بدهم؟"

tokenizer = AutoTokenizer.from_pretrained(
    "intfloat/multilingual-e5-small",
    use_fast=False
)

result = tokenizer(
    text,
    add_special_tokens=True,
    truncation=True,
    max_length=512
)

input_ids = result["input_ids"]

print("Input IDs:")
print(input_ids)

print()
print("Tokens:")
print(tokenizer.convert_ids_to_tokens(input_ids))