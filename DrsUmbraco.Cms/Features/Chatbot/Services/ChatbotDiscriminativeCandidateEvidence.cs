public sealed class ChatbotDiscriminativeCandidateEvidence
{
    public required Guid KnowledgeItemId { get; init; }

    public required string Question { get; init; }

    public required IReadOnlyList<string> DiscriminativeTerms { get; init; }

    public required IReadOnlyList<string> MatchedDiscriminativeTerms { get; init; }

    public required int MatchedDiscriminativeTermCount { get; init; }
}