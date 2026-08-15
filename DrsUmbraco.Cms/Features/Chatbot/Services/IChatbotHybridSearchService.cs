using DrsUmbraco.Cms.Features.Chatbot.Models;

namespace DrsUmbraco.Cms.Features.Chatbot.Services;
public interface IChatbotHybridSearchService
{
    ChatbotHybridSearchResult? FindBest(
        string question);
}