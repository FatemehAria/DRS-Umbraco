using DrsUmbraco.Cms.Features.Chatbot.Models;

namespace DrsUmbraco.Cms.Features.Chatbot.Services;

public sealed class ChatbotMessageService : IChatbotMessageService
{
    private readonly IChatbotMatchingService _matchingService;

    private readonly IChatbotClarificationExactMatchingService
        _clarificationExactMatchingService;

    private readonly IChatbotRerankingService _rerankingService;

    private readonly IChatbotNoMatchDecisionService
        _noMatchDecisionService;

    public ChatbotMessageService(
        IChatbotMatchingService matchingService,
        IChatbotClarificationExactMatchingService
            clarificationExactMatchingService,
        IChatbotRerankingService rerankingService,
        IChatbotNoMatchDecisionService noMatchDecisionService)
    {
        _matchingService =
            matchingService;

        _clarificationExactMatchingService =
            clarificationExactMatchingService;

        _rerankingService =
            rerankingService;

        _noMatchDecisionService =
            noMatchDecisionService;
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
                Reply = clarificationResult.Answer!
            };
        }

        // 3. Find best Answer candidate
        ChatbotRerankResult? rerankingResult =
            _rerankingService.FindBest(
                message,
                ChatbotKnowledgeItemKind.Answer);

        if (rerankingResult is null)
        {
            return CreateNoMatchResult();
        }

        // 4. NoMatch check
        ChatbotNoMatchDecision noMatchDecision =
            _noMatchDecisionService.Decide(
                rerankingResult.SemanticTopScore);

        if (noMatchDecision ==
            ChatbotNoMatchDecision.NoMatch)
        {
            return CreateNoMatchResult();
        }

        // 5. Conservative MVP decision
        if (IsAgreement(rerankingResult))
        {
            return new ChatbotMessageResult
            {
                Reply = rerankingResult.Answer
            };
        }

        // 6. Semantic and Centroid disagree:
        // ask the user to be more specific.
        return new ChatbotMessageResult
        {
            Reply =
                "سؤال شما به چند موضوع نزدیک است. لطفاً کمی دقیق‌تر توضیح دهید."
        };
    }

    private static bool IsAgreement(
        ChatbotRerankResult result)
    {
        return string.Equals(
            result.SelectedStrategy.ToString(),
            "Agreement",
            StringComparison.OrdinalIgnoreCase);
    }

    private static ChatbotMessageResult
        CreateNoMatchResult()
    {
        return new ChatbotMessageResult
        {
            Reply =
                "پاسخ دقیقی برای سؤال شما پیدا نکردم. لطفاً با پشتیبانی تماس بگیرید."
        };
    }
}