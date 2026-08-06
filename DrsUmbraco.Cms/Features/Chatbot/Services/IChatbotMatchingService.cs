using DrsUmbraco.Cms.Features.Chatbot.Models;

namespace DrsUmbraco.Cms.Features.Chatbot.Services;

public interface IChatbotMatchingService
{
    ChatbotMatchResult FindMatch(string? question);
}