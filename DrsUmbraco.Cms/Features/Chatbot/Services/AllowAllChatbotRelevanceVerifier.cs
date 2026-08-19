using DrsUmbraco.Cms.Features.Chatbot.Models;

namespace DrsUmbraco.Cms.Features.Chatbot.Services;

public sealed class AllowAllChatbotRelevanceVerifier
    : IChatbotRelevanceVerifier
{
    public bool IsRelevant(
        string question,
        ChatbotKnowledgeItem candidate)
    {
        return true;
    }
}