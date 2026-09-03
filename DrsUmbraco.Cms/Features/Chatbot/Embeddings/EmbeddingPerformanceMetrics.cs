using System.Diagnostics;
using System.Threading;

namespace DrsUmbraco.Cms.Features.Chatbot.Embeddings;

public sealed class EmbeddingPerformanceMetrics
{
    private long _callCount;
    private long _successCount;
    private long _failureCount;

    private long _tokenizationTicks;
    private long _tokenizationMaxTicks;

    private long _inferenceTicks;
    private long _inferenceMaxTicks;

    private long _postProcessingTicks;
    private long _postProcessingMaxTicks;

    private long _pipelineTicks;
    private long _pipelineMaxTicks;
    private long _firstInferenceTicks;
    public void RecordSuccess(
        long tokenizationTicks,
        long inferenceTicks,
        long postProcessingTicks,
        long pipelineTicks)
    {
        Interlocked.Increment(ref _callCount);
        Interlocked.Increment(ref _successCount);

        AddMeasurement(
            ref _tokenizationTicks,
            ref _tokenizationMaxTicks,
            tokenizationTicks);

        AddMeasurement(
            ref _inferenceTicks,
            ref _inferenceMaxTicks,
            inferenceTicks);

        Interlocked.CompareExchange(
            ref _firstInferenceTicks,
            inferenceTicks,
            comparand: 0);

        AddMeasurement(
            ref _postProcessingTicks,
            ref _postProcessingMaxTicks,
            postProcessingTicks);

        AddMeasurement(
            ref _pipelineTicks,
            ref _pipelineMaxTicks,
            pipelineTicks);
    }

    public void RecordFailure()
    {
        Interlocked.Increment(ref _callCount);
        Interlocked.Increment(ref _failureCount);
    }

    public EmbeddingPerformanceSnapshot Capture()
    {
        return new EmbeddingPerformanceSnapshot(
            Interlocked.Read(ref _callCount),
            Interlocked.Read(ref _successCount),
            Interlocked.Read(ref _failureCount),
            Interlocked.Read(ref _tokenizationTicks),
            Interlocked.Read(ref _tokenizationMaxTicks),
            Interlocked.Read(ref _inferenceTicks),
            Interlocked.Read(ref _inferenceMaxTicks),
            Interlocked.Read(ref _firstInferenceTicks),
            Interlocked.Read(ref _postProcessingTicks),
            Interlocked.Read(ref _postProcessingMaxTicks),
            Interlocked.Read(ref _pipelineTicks),
            Interlocked.Read(ref _pipelineMaxTicks));
    }

    public static double ToMilliseconds(long timestampTicks)
    {
        return timestampTicks * 1000d / Stopwatch.Frequency;
    }

    private static void AddMeasurement(
        ref long totalTicks,
        ref long maxTicks,
        long elapsedTicks)
    {
        Interlocked.Add(
            ref totalTicks,
            elapsedTicks);

        long currentMax =
            Interlocked.Read(ref maxTicks);

        while (elapsedTicks > currentMax)
        {
            long originalValue =
                Interlocked.CompareExchange(
                    ref maxTicks,
                    elapsedTicks,
                    currentMax);

            if (originalValue == currentMax)
            {
                break;
            }

            currentMax = originalValue;
        }
    }
}