namespace DrsUmbraco.Cms.Features.Chatbot.Embeddings;

public interface IChatbotSemanticIndexUpdater
{
    bool Refresh(Guid knowledgeItemId);
}