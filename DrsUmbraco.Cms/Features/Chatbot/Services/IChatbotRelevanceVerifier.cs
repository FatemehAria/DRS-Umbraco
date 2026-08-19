using DrsUmbraco.Cms.Features.Chatbot.Models;

namespace DrsUmbraco.Cms.Features.Chatbot.Services;

public interface IChatbotRelevanceVerifier
{
    bool IsRelevant(
        string question,
        ChatbotKnowledgeItem candidate);
}