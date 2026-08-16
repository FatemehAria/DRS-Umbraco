public interface IChatbotRerankingService
{
    ChatbotRerankResult? FindBest(
        string question,
        ChatbotKnowledgeItemKind? kind = null);
}