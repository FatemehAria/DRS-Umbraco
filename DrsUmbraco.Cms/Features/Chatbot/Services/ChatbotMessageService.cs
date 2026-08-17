using DrsUmbraco.Cms.Features.Chatbot.Models;

namespace DrsUmbraco.Cms.Features.Chatbot.Services;

public sealed class ChatbotMessageService : IChatbotMessageService
{
    private readonly IChatbotMatchingService _matchingService;

    private readonly IChatbotClarificationExactMatchingService _clarificationExactMatchingService;


    private readonly IChatbotNoMatchDecisionService _noMatchDecisionService;

    private readonly IChatbotCandidateEvidenceService _candidateEvidenceService;

    private readonly IChatbotKnowledgeService _knowledgeService;
    public ChatbotMessageService(
        IChatbotMatchingService matchingService,
        IChatbotClarificationExactMatchingService
            clarificationExactMatchingService,
        IChatbotNoMatchDecisionService noMatchDecisionService,
        IChatbotCandidateEvidenceService candidateEvidenceService,
        IChatbotKnowledgeService knowledgeService)
    {
        _matchingService = matchingService;
        _clarificationExactMatchingService = clarificationExactMatchingService;
        _noMatchDecisionService = noMatchDecisionService;
        _candidateEvidenceService = candidateEvidenceService;
        _knowledgeService = knowledgeService;
    }

    public ChatbotMessageResult Process(string message)
    {
        // 1. Exact Answer
        ChatbotMatchResult exactResult =
            _matchingService.FindMatch(message);

        if (exactResult.IsMatch)
        {
            return new ChatbotMessageResult
            {
                ResponseType = ChatbotResponseType.Answer,
                Reply = exactResult.Answer!
            };

        }

        // 2. Exact Clarification
        ChatbotMatchResult clarificationResult =
            _clarificationExactMatchingService.Find(
                message);

        if (clarificationResult.IsMatch)
        {
            return new ChatbotMessageResult
            {
                Reply = clarificationResult.Answer!,
                ResponseType = ChatbotResponseType.Clarification
            };
        }
        // 3. Retrieve Answer candidates once.
        IReadOnlyList<ChatbotCandidateEvidence> candidates =
            _candidateEvidenceService.Find(
                message,
                3,
                ChatbotKnowledgeItemKind.Answer);

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

        foreach (ChatbotCandidateEvidence candidate in candidates)
        {
            if (!addedIds.Add(candidate.KnowledgeItemId))
            {
                continue;
            }

            ChatbotKnowledgeItem? knowledgeItem =
                _knowledgeService.GetById(
                    candidate.KnowledgeItemId);

            if (knowledgeItem is null ||
                knowledgeItem.Kind !=
                    ChatbotKnowledgeItemKind.Answer)
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