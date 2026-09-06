using DrsUmbraco.Cms.Features.Chatbot.Caching;
using DrsUmbraco.Cms.Features.Chatbot.Models;
using DrsUmbraco.Cms.Features.Chatbot.Services;
using System.Diagnostics;

namespace DrsUmbraco.Cms.Features.Chatbot.Embeddings;

public sealed class ChatbotSemanticIndexBuilder
    : IChatbotSemanticIndexBuilder
{
    private readonly IChatbotKnowledgeService _knowledgeService;
    private readonly IChatbotSemanticCandidateFactory _candidateFactory;
    private readonly IChatbotSemanticIndex _semanticIndex;
    private readonly ILogger<ChatbotSemanticIndexBuilder> _logger;
    private readonly EmbeddingPerformanceMetrics _embeddingPerformanceMetrics;
    private readonly IChatbotResponseCache _responseCache;
    public ChatbotSemanticIndexBuilder(
        IChatbotKnowledgeService knowledgeService,
        IChatbotSemanticCandidateFactory candidateFactory,
        IChatbotSemanticIndex semanticIndex,
        ILogger<ChatbotSemanticIndexBuilder> logger,
        EmbeddingPerformanceMetrics embeddingPerformanceMetrics,
        IChatbotResponseCache responseCache)
    {
        _knowledgeService = knowledgeService;
        _candidateFactory = candidateFactory;
        _semanticIndex = semanticIndex;
        _logger = logger;
        _embeddingPerformanceMetrics = embeddingPerformanceMetrics;
        _responseCache = responseCache;
    }

    public int Rebuild()
    {
        Stopwatch knowledgeLoadStopwatch = Stopwatch.StartNew();

        IReadOnlyList<ChatbotKnowledgeItem> knowledgeItems = _knowledgeService.GetAll();

        knowledgeLoadStopwatch.Stop();

        _logger.LogInformation(
            "Performance metric {MetricName} completed in {ElapsedMs} ms with {KnowledgeItemCount} knowledge items.",
            "KnowledgeLoad",
            knowledgeLoadStopwatch.ElapsedMilliseconds,
            knowledgeItems.Count);

        EmbeddingPerformanceSnapshot embeddingMetricsBefore = _embeddingPerformanceMetrics.Capture();

        Stopwatch candidateGenerationStopwatch = Stopwatch.StartNew();

        List<ChatbotSemanticCandidate> candidates = [];

        foreach (ChatbotKnowledgeItem item in knowledgeItems)
        {
            IReadOnlyList<ChatbotSemanticCandidate>
                itemCandidates =
                    _candidateFactory.CreateCandidates(item);

            candidates.AddRange(itemCandidates);
        }


        candidateGenerationStopwatch.Stop();

        EmbeddingPerformanceSnapshot embeddingMetricsAfter = _embeddingPerformanceMetrics.Capture();

        EmbeddingPerformanceSnapshot embeddingMetrics = embeddingMetricsAfter.DifferenceFrom(embeddingMetricsBefore);

        double tokenizationTotalMs = EmbeddingPerformanceMetrics.ToMilliseconds(embeddingMetrics.TokenizationTicks);

        double inferenceTotalMs = EmbeddingPerformanceMetrics.ToMilliseconds(embeddingMetrics.InferenceTicks);

        double postProcessingTotalMs = EmbeddingPerformanceMetrics.ToMilliseconds(embeddingMetrics.PostProcessingTicks);

        double pipelineTotalMs = EmbeddingPerformanceMetrics.ToMilliseconds(embeddingMetrics.PipelineTicks);

        double tokenizationMaxMs = EmbeddingPerformanceMetrics.ToMilliseconds(embeddingMetrics.TokenizationMaxTicks);

        double inferenceMaxMs = EmbeddingPerformanceMetrics.ToMilliseconds(embeddingMetrics.InferenceMaxTicks);

        double postProcessingMaxMs = EmbeddingPerformanceMetrics.ToMilliseconds(embeddingMetrics.PostProcessingMaxTicks);

        double pipelineMaxMs = EmbeddingPerformanceMetrics.ToMilliseconds(embeddingMetrics.PipelineMaxTicks);

        long successCount = embeddingMetrics.SuccessCount;

        double tokenizationAverageMs =
            successCount == 0
                ? 0
                : tokenizationTotalMs / successCount;

        double inferenceAverageMs =
            successCount == 0
                ? 0
                : inferenceTotalMs / successCount;

        double postProcessingAverageMs =
            successCount == 0
                ? 0
                : postProcessingTotalMs / successCount;

        double pipelineAverageMs =
            successCount == 0
                ? 0
                : pipelineTotalMs / successCount;

        double firstInferenceMs =
            EmbeddingPerformanceMetrics.ToMilliseconds(
                embeddingMetrics.FirstInferenceTicks);

        long subsequentInferenceCount =
            firstInferenceMs > 0
                ? Math.Max(
                    embeddingMetrics.SuccessCount - 1,
                    0)
                : embeddingMetrics.SuccessCount;

        double subsequentInferenceTotalMs =
            Math.Max(
                inferenceTotalMs - firstInferenceMs,
                0);

        double subsequentInferenceAverageMs =
            subsequentInferenceCount == 0
                ? 0
                : subsequentInferenceTotalMs /
                  subsequentInferenceCount;

        _logger.LogInformation(
                "Performance metric {MetricName}. " +
                "Calls: {CallCount}, Success: {SuccessCount}, Failed: {FailureCount}. " +
                "Pipeline: Total={PipelineTotalMs:F2} ms, Average={PipelineAverageMs:F3} ms, Max={PipelineMaxMs:F3} ms. " +
                "Tokenization: Total={TokenizationTotalMs:F2} ms, Average={TokenizationAverageMs:F3} ms, Max={TokenizationMaxMs:F3} ms. " +
                "Inference: Total={InferenceTotalMs:F2} ms, Average={InferenceAverageMs:F3} ms, Max={InferenceMaxMs:F3} ms. " +
                "FirstInference={FirstInferenceMs:F3} ms, " +
                "SubsequentInferenceAverage={SubsequentInferenceAverageMs:F3} ms. " +
                "PostProcessing: Total={PostProcessingTotalMs:F2} ms, Average={PostProcessingAverageMs:F3} ms, Max={PostProcessingMaxMs:F3} ms.",
                "EmbeddingBatch",
                embeddingMetrics.CallCount,
                embeddingMetrics.SuccessCount,
                embeddingMetrics.FailureCount,
                pipelineTotalMs,
                pipelineAverageMs,
                pipelineMaxMs,
                tokenizationTotalMs,
                tokenizationAverageMs,
                tokenizationMaxMs,
                inferenceTotalMs,
                inferenceAverageMs,
                inferenceMaxMs,
                firstInferenceMs,
                subsequentInferenceAverageMs,
                postProcessingTotalMs,
                postProcessingAverageMs,
                postProcessingMaxMs);

        _logger.LogInformation(
            "Performance metric {MetricName} completed in {ElapsedMs} ms with {CandidateCount} candidates.",
            "CandidateGeneration",
            candidateGenerationStopwatch.ElapsedMilliseconds,
            candidates.Count);

        Stopwatch indexReplaceStopwatch = Stopwatch.StartNew();

        _semanticIndex.Replace(candidates);

        indexReplaceStopwatch.Stop();

        Stopwatch cacheInvalidationStopwatch = Stopwatch.StartNew();

        _responseCache.Invalidate();

        cacheInvalidationStopwatch.Stop();

        _logger.LogInformation(
            "Performance metric {MetricName} completed in {ElapsedMs} ms with {CandidateCount} candidates.",
            "IndexReplace",
            indexReplaceStopwatch.ElapsedMilliseconds,
            candidates.Count);

        _logger.LogInformation(
            "Performance metric {MetricName} completed in {ElapsedMs} ms.",
            "ChatbotResponseCacheInvalidation",
            cacheInvalidationStopwatch.Elapsed.TotalMilliseconds);

        return candidates.Count;
    }
}