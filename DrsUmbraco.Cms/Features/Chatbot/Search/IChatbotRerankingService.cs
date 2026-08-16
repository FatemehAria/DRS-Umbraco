public interface IChatbotRerankingService
{
    ChatbotRerankResult? FindBest(string question);
}