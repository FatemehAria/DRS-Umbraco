public sealed class ChatbotRerankResult
{
    public required Guid KnowledgeItemId { get; init; }

    public required string Answer { get; init; }

    public required string SelectedStrategy { get; init; }

    public required float SemanticTopScore { get; init; }

    public required float SemanticMargin { get; init; }

    public required float CentroidTopScore { get; init; }

    public required float CentroidMargin { get; init; }
}