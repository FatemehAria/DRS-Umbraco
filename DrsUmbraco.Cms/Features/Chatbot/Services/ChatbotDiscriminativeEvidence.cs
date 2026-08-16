public sealed class ChatbotDiscriminativeEvidence
{
    public required IReadOnlyList<string> UserTerms { get; init; }

    public required IReadOnlyList<ChatbotDiscriminativeCandidateEvidence>
        Candidates
    { get; init; }
}