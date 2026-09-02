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
    public ChatbotSemanticIndexBuilder(
        IChatbotKnowledgeService knowledgeService,
        IChatbotSemanticCandidateFactory candidateFactory,
        IChatbotSemanticIndex semanticIndex,
        ILogger<ChatbotSemanticIndexBuilder> logger)
    {
        _knowledgeService = knowledgeService;
        _candidateFactory = candidateFactory;
        _semanticIndex = semanticIndex;
        _logger = logger;
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

        _logger.LogInformation(
            "Performance metric {MetricName} completed in {ElapsedMs} ms with {CandidateCount} candidates.",
            "CandidateGeneration",
            candidateGenerationStopwatch.ElapsedMilliseconds,
            candidates.Count);

        Stopwatch indexReplaceStopwatch = Stopwatch.StartNew();

        _semanticIndex.Replace(candidates);

        indexReplaceStopwatch.Stop();

        _logger.LogInformation(
            "Performance metric {MetricName} completed in {ElapsedMs} ms with {CandidateCount} candidates.",
            "IndexReplace",
            indexReplaceStopwatch.ElapsedMilliseconds,
            candidates.Count);

        return candidates.Count;
    }
}