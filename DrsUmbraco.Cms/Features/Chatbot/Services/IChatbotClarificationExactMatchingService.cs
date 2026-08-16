using DrsUmbraco.Cms.Features.Chatbot.Models;

namespace DrsUmbraco.Cms.Features.Chatbot.Services;

public interface IChatbotClarificationExactMatchingService
{
    ChatbotMatchResult Find(string question);
}