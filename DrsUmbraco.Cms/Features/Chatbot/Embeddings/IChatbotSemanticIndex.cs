namespace DrsUmbraco.Cms.Features.Chatbot.Embeddings;

public interface IChatbotSemanticIndex
{
    IReadOnlyList<ChatbotSemanticCandidate> GetAll();

    void Replace(
        IReadOnlyList<ChatbotSemanticCandidate> candidates);

    void ReplaceForKnowledgeItem(
        Guid knowledgeItemId,
        IReadOnlyList<ChatbotSemanticCandidate> candidates);

    void RemoveForKnowledgeItem(
        Guid knowledgeItemId);
}