namespace DrsUmbraco.Cms.Features.Chatbot.Embeddings;

public sealed record EmbeddingPerformanceSnapshot(
    long CallCount,
    long SuccessCount,
    long FailureCount,
    long TokenizationTicks,
    long TokenizationMaxTicks,
    long InferenceTicks,
    long InferenceMaxTicks,
    long PostProcessingTicks,
    long PostProcessingMaxTicks,
    long PipelineTicks,
    long PipelineMaxTicks)
{
    public EmbeddingPerformanceSnapshot DifferenceFrom(
        EmbeddingPerformanceSnapshot before)
    {
        return new EmbeddingPerformanceSnapshot(
            CallCount - before.CallCount,
            SuccessCount - before.SuccessCount,
            FailureCount - before.FailureCount,
            TokenizationTicks - before.TokenizationTicks,
            TokenizationMaxTicks,
            InferenceTicks - before.InferenceTicks,
            InferenceMaxTicks,
            PostProcessingTicks - before.PostProcessingTicks,
            PostProcessingMaxTicks,
            PipelineTicks - before.PipelineTicks,
            PipelineMaxTicks);
    }
}