using DrsUmbraco.Cms.Features.Chatbot.Models;

namespace DrsUmbraco.Cms.Features.Chatbot.Services;

public interface IChatbotSemanticRankingService
{
    IReadOnlyList<ChatbotSemanticRankedResult> FindTop(
        string question,
        int limit,
        ChatbotKnowledgeItemKind? kind = null);
}