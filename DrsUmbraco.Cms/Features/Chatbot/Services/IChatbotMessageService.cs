using DrsUmbraco.Cms.Features.Chatbot.Models;

namespace DrsUmbraco.Cms.Features.Chatbot.Services;

public interface IChatbotMessageService
{
    ChatbotMessageResult Process(string message);

    ChatbotMessageResult? SelectSuggestion(Guid knowledgeItemId);
}