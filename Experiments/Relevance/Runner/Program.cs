using System.Diagnostics;
using System.Text;
using Microsoft.ML.OnnxRuntime;
using Microsoft.VisualBasic.FileIO;

string modelPath =
    Path.GetFullPath(
        Path.Combine(
            "..",
            "Model",
            "model-fp32.onnx"));

string tokenizerPath =
    Path.GetFullPath(
        Path.Combine(
            "..",
            "Model",
            "sentencepiece.bpe.model"));

string csvPath =
    Path.GetFullPath(
        Path.Combine(
            "..",
            "chatbot-relevance-verifier-devset.csv"));

var tokenizer =
    new XlmRobertaPairTokenizer(
        tokenizerPath);

string testQuestion =
    "می‌خوام پسورد حسابم رو عوض کنم.";

string testFaq =
    "چطور رمز عبورم را تغییر بدهم؟";

PairModelInput testInput =
    tokenizer.Encode(
        testQuestion,
        testFaq);

long[] officialIds =
[
    0,
    383,
    22914,
    376,
    5797,
    43974,
    14629,
    376,
    3085,
    117372,
    19545,
    5,
    2,
    2,
    116245,
    66888,
    63807,
    376,
    406,
    25012,
    8755,
    1692,
    1245,
    2
];

Console.WriteLine("C# IDS:");
Console.WriteLine(
    "[" +
    string.Join(", ", testInput.InputIds) +
    "]");

Console.WriteLine();

Console.WriteLine(
    $"C# Length: {testInput.InputIds.Length}");

Console.WriteLine(
    $"Official Length: {officialIds.Length}");

bool idsAreEqual =
    testInput.InputIds.SequenceEqual(
        officialIds);

Console.WriteLine();
Console.WriteLine(
    $"Exact match: {idsAreEqual}");

if (!idsAreEqual)
{
    int maxLength =
        Math.Max(
            testInput.InputIds.Length,
            officialIds.Length);

    for (int i = 0; i < maxLength; i++)
    {
        long? actual =
            i < testInput.InputIds.Length
                ? testInput.InputIds[i]
                : null;

        long? expected =
            i < officialIds.Length
                ? officialIds[i]
                : null;

        if (actual != expected)
        {
            Console.WriteLine(
                $"First mismatch at index {i}: " +
                $"C#={actual}, Official={expected}");

            break;
        }
    }
}

using InferenceSession session =
    new(modelPath);

float Score(
    string question,
    string candidateFaq)
{
    PairModelInput input =
        tokenizer.Encode(
            question,
            candidateFaq);

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

    using IDisposableReadOnlyCollection<OrtValue>
        outputs =
            session.Run(
                runOptions,
                inputs,
                session.OutputNames);

    ReadOnlySpan<float> logits =
        outputs[0]
            .GetTensorDataAsSpan<float>();

    return logits[0];
}

List<(string Id, string Label, float Score)> results =
    [];

using TextFieldParser parser =
    new(
        csvPath,
        Encoding.UTF8);

parser.TextFieldType =
    FieldType.Delimited;

parser.SetDelimiters(",");

parser.HasFieldsEnclosedInQuotes =
    true;

// Header
parser.ReadFields();

Stopwatch stopwatch =
    Stopwatch.StartNew();

while (!parser.EndOfData)
{
    string[]? fields =
        parser.ReadFields();

    if (fields is null ||
        fields.Length < 4)
    {
        continue;
    }

    string id =
        fields[0];

    string label =
        fields[1];

    string userQuestion =
        fields[2];

    string candidateFaq =
        fields[3];

    float score =
        Score(
            userQuestion,
            candidateFaq);

    results.Add(
        (
            id,
            label,
            score
        ));

    Console.WriteLine(
        $"{id,-4} {label,-8} {score,10:F6}");
}

stopwatch.Stop();

Console.WriteLine();
Console.WriteLine(
    "==============================");

Console.WriteLine(
    "SUMMARY");

Console.WriteLine(
    "==============================");

float[] positiveScores =
    results
        .Where(x =>
            x.Label == "Positive")
        .Select(x => x.Score)
        .ToArray();

float[] negativeScores =
    results
        .Where(x =>
            x.Label == "Negative")
        .Select(x => x.Score)
        .ToArray();

Console.WriteLine();
Console.WriteLine("Positive:");

Console.WriteLine(
    $"  Min: {positiveScores.Min():F6}");

Console.WriteLine(
    $"  Max: {positiveScores.Max():F6}");

Console.WriteLine(
    $"  Avg: {positiveScores.Average():F6}");

Console.WriteLine();

Console.WriteLine("Negative:");

Console.WriteLine(
    $"  Min: {negativeScores.Min():F6}");

Console.WriteLine(
    $"  Max: {negativeScores.Max():F6}");

Console.WriteLine(
    $"  Avg: {negativeScores.Average():F6}");

Console.WriteLine();

Console.WriteLine(
    $"Gap: {positiveScores.Min() - negativeScores.Max():F6}");

Console.WriteLine();

Console.WriteLine(
    $"Total time: {stopwatch.ElapsedMilliseconds} ms");

Console.WriteLine(
    $"Average per pair: " +
    $"{stopwatch.Elapsed.TotalMilliseconds / results.Count:F2} ms");

Console.WriteLine();

Console.WriteLine(
    "Scores from highest to lowest:");

foreach (var result in
         results.OrderByDescending(
             x => x.Score))
{
    Console.WriteLine(
        $"{result.Id,-4} " +
        $"{result.Label,-8} " +
        $"{result.Score,10:F6}");
}