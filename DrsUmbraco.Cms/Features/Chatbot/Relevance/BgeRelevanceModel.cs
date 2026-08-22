using Microsoft.ML.OnnxRuntime;

namespace DrsUmbraco.Cms.Features.Chatbot.Relevance;

public sealed class BgeRelevanceModel : IDisposable
{
    private readonly InferenceSession _session;

    public BgeRelevanceModel(string modelPath)
    {
        if (string.IsNullOrWhiteSpace(modelPath))
        {
            throw new ArgumentException(
                "Model path is required.",
                nameof(modelPath));
        }

        if (!File.Exists(modelPath))
        {
            throw new FileNotFoundException(
                "BGE relevance model was not found.",
                modelPath);
        }

        _session = new InferenceSession(modelPath);
    }

    public float Run(BgeRelevanceModelInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (input.InputIds.Length == 0)
        {
            throw new ArgumentException(
                "Input IDs cannot be empty.",
                nameof(input));
        }

        if (input.InputIds.Length != input.AttentionMask.Length)
        {
            throw new ArgumentException(
                "Input IDs and attention mask must have the same length.",
                nameof(input));
        }

        long[] shape =
        [
            1,
            input.InputIds.Length
        ];

        using OrtValue inputIds =
            OrtValue.CreateTensorValueFromMemory(
                input.InputIds,
                shape);

        using OrtValue attentionMask =
            OrtValue.CreateTensorValueFromMemory(
                input.AttentionMask,
                shape);

        Dictionary<string, OrtValue> modelInputs =
            new()
            {
                ["input_ids"] = inputIds,
                ["attention_mask"] = attentionMask
            };

        using RunOptions runOptions = new();

        using IDisposableReadOnlyCollection<OrtValue> outputs =
            _session.Run(
                runOptions,
                modelInputs,
                _session.OutputNames);

        ReadOnlySpan<float> logits =
            outputs[0]
                .GetTensorDataAsSpan<float>();

        if (logits.Length == 0)
        {
            throw new InvalidOperationException(
                "BGE model did not return a relevance score.");
        }

        return logits[0];
    }

    public void Dispose()
    {
        _session.Dispose();
    }
}