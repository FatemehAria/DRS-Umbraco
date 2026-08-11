namespace DrsUmbraco.Cms.Features.Chatbot.Embeddings;

public interface IChatbotSemanticIndex
{
    IReadOnlyList<ChatbotSemanticCandidate> GetAll();

    void Replace(
        IReadOnlyList<ChatbotSemanticCandidate> candidates);
}