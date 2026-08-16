namespace DrsUmbraco.Cms.Features.Chatbot.Embeddings;

public interface IChatbotSemanticSearchService
{
    ChatbotSemanticSearchResult? FindBest(
        string question,
        ChatbotKnowledgeItemKind? kind = null);
}