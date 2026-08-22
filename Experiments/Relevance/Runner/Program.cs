using Microsoft.ML.OnnxRuntime;

string modelPath =
    Path.GetFullPath(
        Path.Combine(
            "..",
            "Model-BGE",
            "model.onnx"));

string tokenizerPath =
    Path.GetFullPath(
        Path.Combine(
            "..",
            "Model-BGE",
            "sentencepiece.bpe.model"));

var tokenizer =
    new XlmRobertaPairTokenizer(
        tokenizerPath);

using InferenceSession session =
    new(modelPath);

string question =
    "می‌خوام پسورد حسابم رو عوض کنم.";

string faq =
    "چطور رمز عبورم را تغییر بدهم؟";

PairModelInput input =
    tokenizer.Encode(
        question,
        faq);

long[] shape =
[
    1,
    input.InputIds.Length
];

using OrtValue inputIdsOrt =
    OrtValue.CreateTensorValueFromMemory(
        input.InputIds,
        shape);

using OrtValue attentionMaskOrt =
    OrtValue.CreateTensorValueFromMemory(
        input.AttentionMask,
        shape);

Dictionary<string, OrtValue> inputs =
    new()
    {
        ["input_ids"] = inputIdsOrt,
        ["attention_mask"] = attentionMaskOrt
    };

using RunOptions runOptions =
    new();

using IDisposableReadOnlyCollection<OrtValue> outputs =
    session.Run(
        runOptions,
        inputs,
        session.OutputNames);

ReadOnlySpan<float> logits =
    outputs[0]
        .GetTensorDataAsSpan<float>();

float score =
    logits[0];

Console.WriteLine(
    $"Input length: {input.InputIds.Length}");

Console.WriteLine(
    $"BGE C# score: {score:F9}");