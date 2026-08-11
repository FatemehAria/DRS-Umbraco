namespace DrsUmbraco.Cms.Features.Chatbot.Embeddings;

public sealed class ChatbotSemanticDecision
{
    public required ChatbotSemanticDecisionType Type { get; init; }

    public ChatbotSemanticSearchResult? SearchResult { get; init; }
}