using DrsUmbraco.Cms.Features.Chatbot.Models;
using DrsUmbraco.Cms.Features.Chatbot.Text;

namespace DrsUmbraco.Cms.Features.Chatbot.Services;

public sealed class ChatbotClarificationExactMatchingService
    : IChatbotClarificationExactMatchingService
{
    private readonly IChatbotKnowledgeService
        _knowledgeService;

    private readonly IPersianTextNormalizer
        _normalizer;

    public ChatbotClarificationExactMatchingService(
        IChatbotKnowledgeService knowledgeService,
        IPersianTextNormalizer normalizer)
    {
        _knowledgeService =
            knowledgeService;

        _normalizer =
            normalizer;
    }

    public ChatbotMatchResult Find(
        string question)
    {
        string normalizedQuestion =
            _normalizer.Normalize(question);

        if (string.IsNullOrWhiteSpace(
            normalizedQuestion))
        {
            return NoMatch();
        }

        IReadOnlyList<ChatbotKnowledgeItem> items =
            _knowledgeService.GetAll();

        foreach (ChatbotKnowledgeItem item in items)
        {
            if (item.Kind !=
                ChatbotKnowledgeItemKind.Clarification)
            {
                continue;
            }

            if (IsExactMatch(
                normalizedQuestion,
                item.Question))
            {
                return Match(item);
            }

            foreach (string alternativeQuestion
                in item.AlternativeQuestions)
            {
                if (IsExactMatch(
                    normalizedQuestion,
                    alternativeQuestion))
                {
                    return Match(item);
                }
            }
        }

        return NoMatch();
    }

    private bool IsExactMatch(
        string normalizedQuestion,
        string candidate)
    {
        string normalizedCandidate =
            _normalizer.Normalize(candidate);

        return string.Equals(
            normalizedQuestion,
            normalizedCandidate,
            StringComparison.Ordinal);
    }

    private static ChatbotMatchResult Match(
        ChatbotKnowledgeItem item)
    {
        return new ChatbotMatchResult
        {
            IsMatch = true,
            KnowledgeItemId = item.Id,
            Answer = item.Answer
        };
    }

    private static ChatbotMatchResult NoMatch()
    {
        return new ChatbotMatchResult
        {
            IsMatch = false,
            KnowledgeItemId = null,
            Answer = null
        };
    }
}