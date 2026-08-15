namespace DrsUmbraco.Cms.Features.Chatbot.Models;

public sealed class ChatbotSemanticRankedResult
{
    public required int Rank { get; init; }

    public required Guid KnowledgeItemId { get; init; }

    public required string MatchedText { get; init; }

    public required string Answer { get; init; }

    public required float Score { get; init; }
}