public sealed class ChatbotWinnerDiscriminativeEvidence
{
    public ChatbotRerankResult? Winner { get; init; }

    public ChatbotDiscriminativeCandidateEvidence?
        WinnerEvidence
    { get; init; }

    public required IReadOnlyList<string>
        UserTerms
    { get; init; }
}