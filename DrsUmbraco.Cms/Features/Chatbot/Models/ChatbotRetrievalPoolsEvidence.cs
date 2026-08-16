using DrsUmbraco.Cms.Features.Chatbot.Services;

namespace DrsUmbraco.Cms.Features.Chatbot.Models;

public sealed class ChatbotRetrievalPoolsEvidence
{
    public required IReadOnlyList<ChatbotCandidateEvidence>
        Answers
    { get; init; }

    public required IReadOnlyList<ChatbotCandidateEvidence>
        Clarifications
    { get; init; }
}