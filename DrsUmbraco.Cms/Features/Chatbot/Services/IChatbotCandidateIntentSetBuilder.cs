using DrsUmbraco.Cms.Features.Chatbot.Services;

public interface IChatbotCandidateIntentSetBuilder
{
    IReadOnlyList<ChatbotCandidateIntent> Build(
        IReadOnlyList<ChatbotCandidateEvidence> evidence);
}