public sealed class ChatbotCandidateIntent
{
    public required Guid KnowledgeItemId { get; init; }

    public required string Answer { get; init; }

    public required int Top1SupportCount { get; init; }

    public required int Top2SupportCount { get; init; }

    public required int BestRank { get; init; }
}