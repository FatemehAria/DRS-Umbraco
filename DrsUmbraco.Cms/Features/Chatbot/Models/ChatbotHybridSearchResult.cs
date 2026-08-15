namespace DrsUmbraco.Cms.Features.Chatbot.Models;
public sealed class ChatbotHybridSearchResult
{
    public required Guid KnowledgeItemId { get; init; }

    public required string Answer { get; init; }

    public required string MatchedText { get; init; }

    public required float SemanticScore { get; init; }

    public required float LexicalScore { get; init; }

    public required float Score { get; init; }

    public Guid? SecondBestKnowledgeItemId { get; init; }

    public float? SecondBestScore { get; init; }

    public float? Margin { get; init; }
}