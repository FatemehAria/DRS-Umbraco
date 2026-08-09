namespace DrsUmbraco.Cms.Features.Chatbot.Embeddings;

public sealed class LocalE5EmbeddingService
    : IEmbeddingService
{
    private readonly ILocalEmbeddingTokenizer _tokenizer;
    private readonly LocalEmbeddingModel _model;

    public LocalE5EmbeddingService(
        ILocalEmbeddingTokenizer tokenizer,
        LocalEmbeddingModel model)
    {
        _tokenizer = tokenizer;
        _model = model;
    }

    public float[] Generate(string text)
    {
        // 1. اعتبارسنجی
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException(
                "Text is required.",
                nameof(text));
        }
        // 2. اضافه کردن query:
        string modelText = $"query: {text.Trim()}";
        // 3. Tokenize
        EmbeddingModelInput input = _tokenizer.Encode(modelText);
        // 4. اجرای ONNX
        EmbeddingModelRawOutput output = _model.Run(input);
        // 5. Mean Pooling
        float[] embedding = MeanPool(output, input.AttentionMask);
        // 6. L2 Normalization
        NormalizeL2(embedding);
        // 7. return
        return embedding;

    }

    private static float[] MeanPool(
    EmbeddingModelRawOutput output,
    long[] attentionMask)
    {
        int sequenceLength =
            checked((int)output.Shape[1]);

        int hiddenSize =
            checked((int)output.Shape[2]);

        if (attentionMask.Length != sequenceLength)
        {
            throw new ArgumentException(
                "Attention mask length must match sequence length.",
                nameof(attentionMask));
        }

        float[] pooled =
            new float[hiddenSize];

        int includedTokenCount = 0;

        for (int tokenIndex = 0;
             tokenIndex < sequenceLength;
             tokenIndex++)
        {
            if (attentionMask[tokenIndex] == 0)
            {
                continue;
            }

            includedTokenCount++;

            for (int dimensionIndex = 0;
                 dimensionIndex < hiddenSize;
                 dimensionIndex++)
            {
                // توکن شماره فلان در دایمنشن فلان کجای آرایه 4224 هست
                int valueIndex =
                    tokenIndex * hiddenSize + dimensionIndex;

                pooled[dimensionIndex] +=
                    output.Values[valueIndex];
            }
        }

        if (includedTokenCount == 0)
        {
            throw new InvalidOperationException(
                "No valid tokens were available for mean pooling.");
        }

        for (int dimensionIndex = 0;
             dimensionIndex < hiddenSize;
             dimensionIndex++)
        {
            pooled[dimensionIndex] /=
                includedTokenCount;
        }

        return pooled;
    }

    private static void NormalizeL2(float[] vector)
    {
        double sumOfSquares = 0;

        foreach (float value in vector)
        {
            sumOfSquares += value * value;
        }

        double magnitude =
            Math.Sqrt(sumOfSquares);

        if (magnitude == 0)
        {
            return;
        }

        for (int i = 0; i < vector.Length; i++)
        {
            vector[i] =
                (float)(vector[i] / magnitude);
        }
    }
}