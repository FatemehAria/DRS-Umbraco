namespace DrsUmbraco.Cms.Features.Chatbot.Services;

public interface IChatbotCandidateEvidenceService
{
    IReadOnlyList<ChatbotCandidateEvidence> Find(
        string question,
        int limit,
        ChatbotKnowledgeItemKind? kind = null);
}