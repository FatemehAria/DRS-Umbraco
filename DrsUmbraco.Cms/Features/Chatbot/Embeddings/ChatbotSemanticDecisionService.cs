namespace DrsUmbraco.Cms.Features.Chatbot.Embeddings;

public sealed class ChatbotSemanticDecisionService
    : IChatbotSemanticDecisionService
{
    private const float MinimumScore = 0.88f;
    private const float MinimumMargin = 0.05f;

    public ChatbotSemanticDecision Decide(
        ChatbotSemanticSearchResult? result)
    {
        if (result is null)
        {
            return new ChatbotSemanticDecision
            {
                Type = ChatbotSemanticDecisionType.NoMatch
            };
        }

        if (result.Score < MinimumScore)
        {
            return new ChatbotSemanticDecision
            {
                Type = ChatbotSemanticDecisionType.NoMatch,
                SearchResult = result
            };
        }

        if (result.Margin is not null &&
            result.Margin < MinimumMargin)
        {
            return new ChatbotSemanticDecision
            {
                Type = ChatbotSemanticDecisionType.Ambiguous,
                SearchResult = result
            };
        }

        return new ChatbotSemanticDecision
        {
            Type = ChatbotSemanticDecisionType.Confident,
            SearchResult = result
        };
    }
}