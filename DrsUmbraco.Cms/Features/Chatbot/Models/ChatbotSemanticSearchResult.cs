namespace DrsUmbraco.Cms.Features.Chatbot.Embeddings;

public sealed class ChatbotSemanticSearchResult
{
    public required Guid KnowledgeItemId { get; init; }

    public required string Answer { get; init; }

    public required string MatchedText { get; init; }

    public required float Score { get; init; }

    public Guid? SecondBestKnowledgeItemId { get; init; }

    public float? SecondBestScore { get; init; }

    public float? Margin { get; init; }
}