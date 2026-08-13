using DrsUmbraco.Cms.Features.Chatbot.Embeddings;
using DrsUmbraco.Cms.Features.Chatbot.Models;

namespace DrsUmbraco.Cms.Features.Chatbot.Services;

public sealed class ChatbotMessageService : IChatbotMessageService
{
    private readonly IChatbotMatchingService _matchingService;
    private readonly IChatbotSemanticSearchService _semanticSearchService;
    private readonly IChatbotSemanticDecisionService _semanticDecisionService;
    public ChatbotMessageService(
        IChatbotMatchingService matchingService,
        IChatbotSemanticSearchService semanticSearchService,
        IChatbotSemanticDecisionService semanticDecisionService)
    {
        _matchingService = matchingService;
        _semanticSearchService = semanticSearchService;
        _semanticDecisionService = semanticDecisionService;
    }

    public ChatbotMessageResult Process(string message)
    {
        // 1. Exact Match
        ChatbotMatchResult exactResult =
            _matchingService.FindMatch(message);

        if (exactResult.IsMatch &&
            exactResult.Answer is string exactAnswer)
        {
            return new ChatbotMessageResult
            {
                Reply = exactAnswer
            };
        }

        // 2. Semantic Search
        ChatbotSemanticSearchResult? semanticResult =
            _semanticSearchService.FindBest(message);

        // 3. Decide whether semantic result is trustworthy
        ChatbotSemanticDecision decision =
            _semanticDecisionService.Decide(semanticResult);

        // 4. Return appropriate response
        if (decision.Type ==
                ChatbotSemanticDecisionType.Confident &&
            decision.SearchResult?.Answer is string semanticAnswer)
        {
            return new ChatbotMessageResult
            {
                Reply = semanticAnswer
            };
        }

        if (decision.Type ==
            ChatbotSemanticDecisionType.Ambiguous)
        {
            return new ChatbotMessageResult
            {
                Reply =
                    "سؤال شما به چند موضوع نزدیک است. لطفاً کمی دقیق‌تر توضیح دهید."
            };
        }

        return new ChatbotMessageResult
        {
            Reply =
                "پاسخ دقیقی برای سؤال شما پیدا نکردم. لطفاً با پشتیبانی تماس بگیرید."
        };
    }
}