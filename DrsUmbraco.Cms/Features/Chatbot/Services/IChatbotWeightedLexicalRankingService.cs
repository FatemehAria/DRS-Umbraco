using DrsUmbraco.Cms.Features.Chatbot.Search;

namespace DrsUmbraco.Cms.Features.Chatbot.Services;

public interface IChatbotWeightedLexicalRankingService
{
    IReadOnlyList<ChatbotLexicalRankedResult> FindTop(
        string question,
        int limit,
        ChatbotKnowledgeItemKind? kind = null);
}