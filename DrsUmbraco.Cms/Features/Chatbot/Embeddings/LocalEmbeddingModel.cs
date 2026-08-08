using Microsoft.ML.OnnxRuntime;

namespace DrsUmbraco.Cms.Features.Chatbot.Embeddings;

public sealed class LocalEmbeddingModel : IDisposable
{
    private readonly InferenceSession _session;

    public LocalEmbeddingModel(
        IWebHostEnvironment environment)
    {
        // 1. ساخت مسیر model.onnx
        string modelPath = Path.Combine(
            environment.ContentRootPath,
            "AIModel",
            "multilingual-e5-small",
            "model.onnx");
        // 2. بررسی وجود فایل
        if (!File.Exists(modelPath))
        {
            throw new FileNotFoundException(
                "Embedding model was not found.",
                modelPath);
        }
        // 3. ساخت InferenceSession
        _session = new InferenceSession(modelPath);
    }

    public void Dispose()
    {
        _session.Dispose();
    }

    public IReadOnlyCollection<string> GetInputNames()
    {
        return _session.InputMetadata.Keys.ToArray();
    }

    public IReadOnlyCollection<string> GetOutputNames()
    {
        return _session.OutputMetadata.Keys.ToArray();
    }

    public EmbeddingModelRawOutput Run(
    EmbeddingModelInput input)
    {
        // inference را می‌نویسیم

        if (input.InputIds.Length != input.AttentionMask.Length ||
            input.InputIds.Length != input.TokenTypeIds.Length)
        {
            throw new ArgumentException(
                "Embedding model input arrays must have the same length.",
                nameof(input));
        }

        long[] inputShape =
        [
            1,
            input.InputIds.Length
        ];
        
        using OrtValue inputIdsValue =
            OrtValue.CreateTensorValueFromMemory(
                input.InputIds,
                inputShape);

        using OrtValue attentionMaskValue =
            OrtValue.CreateTensorValueFromMemory(
                input.AttentionMask,
                inputShape);

        using OrtValue tokenTypeIdsValue =
            OrtValue.CreateTensorValueFromMemory(
                input.TokenTypeIds,
                inputShape);

        var inputs = new Dictionary<string, OrtValue>
        {
            ["input_ids"] = inputIdsValue,
            ["attention_mask"] = attentionMaskValue,
            ["token_type_ids"] = tokenTypeIdsValue
        };

        using RunOptions runOptions = new();

        using IDisposableReadOnlyCollection<OrtValue> outputs =
            _session.Run(
                runOptions,
                inputs,
                _session.OutputNames);

        OrtValue output = outputs[0];

        OrtTensorTypeAndShapeInfo outputInfo = output.GetTensorTypeAndShape();

        long[] outputShape = outputInfo.Shape;

        float[] values = output.GetTensorDataAsSpan<float>().ToArray();

        return new EmbeddingModelRawOutput
        {
            Values = values,
            Shape = outputShape
        };
    }
}