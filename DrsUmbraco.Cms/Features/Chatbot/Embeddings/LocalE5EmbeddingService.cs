using System.Diagnostics;

namespace DrsUmbraco.Cms.Features.Chatbot.Embeddings;

public sealed class LocalE5EmbeddingService
    : IEmbeddingService
{
    private readonly ILocalEmbeddingTokenizer _tokenizer;
    private readonly LocalEmbeddingModel _model;
    private readonly EmbeddingPerformanceMetrics _performanceMetrics;

    public LocalE5EmbeddingService(
        ILocalEmbeddingTokenizer tokenizer,
        LocalEmbeddingModel model,
        EmbeddingPerformanceMetrics performanceMetrics)
    {
        _tokenizer = tokenizer;
        _model = model;
        _performanceMetrics = performanceMetrics;
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

        long pipelineStartedAt = Stopwatch.GetTimestamp();

        try
        {

            // 2. اضافه کردن query:
            string modelText = $"query: {text.Trim()}";

            long tokenizationStartedAt =
                Stopwatch.GetTimestamp();

            // 3. Tokenize
            EmbeddingModelInput input = _tokenizer.Encode(modelText);

            long tokenizationTicks =
                Stopwatch.GetTimestamp() -
                tokenizationStartedAt;

            long inferenceStartedAt =
                Stopwatch.GetTimestamp();

            // 4. اجرای ONNX
            EmbeddingModelRawOutput output = _model.Run(input);

            long inferenceTicks =
                Stopwatch.GetTimestamp() -
                inferenceStartedAt;

            long postProcessingStartedAt =
                Stopwatch.GetTimestamp();


            // 5. Mean Pooling
            float[] embedding = MeanPool(output, input.AttentionMask);

            // 6. L2 Normalization
            NormalizeL2(embedding);

            long postProcessingTicks =
                Stopwatch.GetTimestamp() -
                postProcessingStartedAt;

            long pipelineTicks =
                Stopwatch.GetTimestamp() -
                pipelineStartedAt;

            _performanceMetrics.RecordSuccess(
                tokenizationTicks,
                inferenceTicks,
                postProcessingTicks,
                pipelineTicks);


            // 7. return
            return embedding;
        }
        catch
        {
            _performanceMetrics.RecordFailure();
            throw;
        }

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