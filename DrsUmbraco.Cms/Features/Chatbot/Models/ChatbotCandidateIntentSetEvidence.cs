namespace DrsUmbraco.Cms.Features.Chatbot.Models;

public sealed class ChatbotCandidateIntentSetEvidence
{
    public required IReadOnlyList<ChatbotCandidateIntent>
        Candidates
    { get; init; }

    public required int CandidateCount { get; init; }

    public required int MaxTop1SupportCount { get; init; }

    public required int SecondTop1SupportCount { get; init; }

    public required int Top1SupportGap { get; init; }
}