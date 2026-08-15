namespace DrsUmbraco.Cms.Features.Chatbot.Search;

public sealed class ChatbotLexicalRankedResult
{
    public required int Rank { get; init; }

    public required Guid KnowledgeItemId { get; init; }

    public required string MatchedText { get; init; }

    public required string Answer { get; init; }

    public required float Score { get; init; }
}