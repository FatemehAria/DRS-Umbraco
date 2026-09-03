using System.Diagnostics;
using DrsUmbraco.Cms.Features.Chatbot.Models;

namespace DrsUmbraco.Cms.Features.Chatbot.Services;

public sealed class ChatbotMessageService : IChatbotMessageService
{
    private readonly IChatbotMatchingService _matchingService;
    private readonly IChatbotClarificationExactMatchingService _clarificationExactMatchingService;
    private readonly IChatbotNoMatchDecisionService _noMatchDecisionService;
    private readonly IChatbotCandidateEvidenceService _candidateEvidenceService;
    private readonly IChatbotKnowledgeService _knowledgeService;
    private readonly IChatbotRelevanceVerifier _relevanceVerifier;
    private readonly ILogger<ChatbotMessageService> _logger;
    public ChatbotMessageService(
        IChatbotMatchingService matchingService,
        IChatbotClarificationExactMatchingService
            clarificationExactMatchingService,
        IChatbotNoMatchDecisionService noMatchDecisionService,
        IChatbotCandidateEvidenceService candidateEvidenceService,
        IChatbotKnowledgeService knowledgeService,
        IChatbotRelevanceVerifier relevanceVerifier,
        ILogger<ChatbotMessageService> logger)
    {
        _matchingService = matchingService;
        _clarificationExactMatchingService = clarificationExactMatchingService;
        _noMatchDecisionService = noMatchDecisionService;
        _candidateEvidenceService = candidateEvidenceService;
        _knowledgeService = knowledgeService;
        _relevanceVerifier = relevanceVerifier;
        _logger = logger;
    }

    public ChatbotMessageResult Process(string message)
    {
        Stopwatch exactMatchingStopwatch = Stopwatch.StartNew();

        // 1. Exact Answer
        ChatbotMatchResult exactResult =
            _matchingService.FindMatch(message);

        exactMatchingStopwatch.Stop();

        _logger.LogInformation(
            "Performance metric {MetricName} completed in {ElapsedMs} ms. IsMatch={IsMatch}.",
            "ExactMatching",
            exactMatchingStopwatch.Elapsed.TotalMilliseconds,
            exactResult.IsMatch);

        if (exactResult.IsMatch)
        {
            return new ChatbotMessageResult
            {
                ResponseType = ChatbotResponseType.Answer,
                Reply = exactResult.Answer!
            };

        }

        Stopwatch clarificationStopwatch = Stopwatch.StartNew();

        // 2. Exact Clarification
        ChatbotMatchResult clarificationResult =
            _clarificationExactMatchingService.Find(
                message);

        clarificationStopwatch.Stop();

        _logger.LogInformation(
            "Performance metric {MetricName} completed in {ElapsedMs} ms. IsMatch={IsMatch}.",
            "ClarificationExactMatching",
            clarificationStopwatch.Elapsed.TotalMilliseconds,
            clarificationResult.IsMatch);

        if (clarificationResult.IsMatch)
        {
            return new ChatbotMessageResult
            {
                Reply = clarificationResult.Answer!,
                ResponseType = ChatbotResponseType.Clarification
            };
        }

        Stopwatch candidateEvidenceStopwatch = Stopwatch.StartNew();

        // 3. Retrieve Answer candidates once.
        IReadOnlyList<ChatbotCandidateEvidence> candidates =
            _candidateEvidenceService.Find(
                message,
                3,
                ChatbotKnowledgeItemKind.Answer);

        candidateEvidenceStopwatch.Stop();

        _logger.LogInformation(
            "Performance metric {MetricName} completed in {ElapsedMs} ms with {CandidateCount} candidates.",
            "CandidateEvidence",
            candidateEvidenceStopwatch.Elapsed.TotalMilliseconds,
            candidates.Count);

        if (candidates.Count == 0)
        {
            return CreateNoMatchResult();
        }

        float semanticTopScore =
            candidates
                .Where(candidate =>
                    candidate.SemanticScore.HasValue)
                .Select(candidate =>
                    candidate.SemanticScore!.Value)
                .DefaultIfEmpty(0f)
                .Max();

        ChatbotNoMatchDecision noMatchDecision = _noMatchDecisionService.Decide(semanticTopScore);

        if (noMatchDecision ==
            ChatbotNoMatchDecision.NoMatch)
        {
            return CreateNoMatchResult();
        }

        List<ChatbotSuggestion> suggestions = [];

        HashSet<Guid> addedIds = [];

        double knowledgeLookupTotalMs = 0;
        double relevanceVerificationTotalMs = 0;
        double relevanceVerificationMaxMs = 0;

        int knowledgeLookupCount = 0;
        int relevanceVerificationCount = 0;

        foreach (ChatbotCandidateEvidence candidate in candidates)
        {
            if (!addedIds.Add(candidate.KnowledgeItemId))
            {
                continue;
            }

            Stopwatch knowledgeLookupStopwatch =
                Stopwatch.StartNew();

            ChatbotKnowledgeItem? knowledgeItem =
                _knowledgeService.GetById(
                    candidate.KnowledgeItemId);

            knowledgeLookupStopwatch.Stop();

            knowledgeLookupCount++;

            knowledgeLookupTotalMs += knowledgeLookupStopwatch.Elapsed.TotalMilliseconds;

            if (knowledgeItem is null ||
                knowledgeItem.Kind !=
                    ChatbotKnowledgeItemKind.Answer)
            {
                continue;
            }

            Stopwatch relevanceStopwatch = Stopwatch.StartNew();

            bool isRelevant =
                _relevanceVerifier.IsRelevant(
                    message,
                    knowledgeItem);

            relevanceStopwatch.Stop();

            double relevanceElapsedMs = relevanceStopwatch.Elapsed.TotalMilliseconds;

            relevanceVerificationCount++;

            relevanceVerificationTotalMs += relevanceElapsedMs;

            relevanceVerificationMaxMs =
                Math.Max(
                    relevanceVerificationMaxMs,
                    relevanceElapsedMs);

            if (!isRelevant)
            {
                continue;
            }

            suggestions.Add(
                new ChatbotSuggestion
                {
                    KnowledgeItemId =
                        knowledgeItem.Id,

                    Label =
                        knowledgeItem.Question
                });

            if (suggestions.Count == 3)
            {
                break;
            }
        }

        _logger.LogInformation(
            "Performance metric {MetricName}. " +
            "KnowledgeLookupCount={KnowledgeLookupCount}, KnowledgeLookupTotalMs={KnowledgeLookupTotalMs}. " +
            "VerificationCount={VerificationCount}, VerificationTotalMs={VerificationTotalMs}, " +
            "VerificationAverageMs={VerificationAverageMs}, VerificationMaxMs={VerificationMaxMs}.",
            "SuggestionEvaluation",
            knowledgeLookupCount,
            knowledgeLookupTotalMs,
            relevanceVerificationCount,
            relevanceVerificationTotalMs,
            relevanceVerificationCount == 0
                ? 0
                : relevanceVerificationTotalMs /
                relevanceVerificationCount,
            relevanceVerificationMaxMs);

        if (suggestions.Count == 0)
        {
            return CreateNoMatchResult();
        }

        return new ChatbotMessageResult
        {
            ResponseType =
                ChatbotResponseType.Suggestions,

            Reply =
                "منظورتان کدام مورد است؟",

            Suggestions =
                suggestions
        };
    }

    public ChatbotMessageResult? SelectSuggestion(
        Guid knowledgeItemId)
    {
        ChatbotKnowledgeItem? item =
            _knowledgeService.GetById(
                knowledgeItemId);

        if (item is null ||
            item.Kind != ChatbotKnowledgeItemKind.Answer)
        {
            return null;
        }

        return new ChatbotMessageResult
        {
            ResponseType = ChatbotResponseType.Answer,
            Reply = item.Answer
        };
    }
    private static ChatbotMessageResult
    CreateNoMatchResult()
    {
        return new ChatbotMessageResult
        {
            Reply =
                "پاسخ دقیقی برای سؤال شما پیدا نکردم. لطفاً با پشتیبانی تماس بگیرید.",
            ResponseType = ChatbotResponseType.Fallback
        };
    }
}