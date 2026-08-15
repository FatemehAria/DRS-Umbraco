using DrsUmbraco.Cms.Features.Chatbot.Models;

public interface IChatbotWeightedHybridSearchService
{
    ChatbotHybridSearchResult? FindBest(
        string question);
}