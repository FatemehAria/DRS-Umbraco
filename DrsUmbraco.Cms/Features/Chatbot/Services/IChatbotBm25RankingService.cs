using DrsUmbraco.Cms.Features.Chatbot.Search;

namespace DrsUmbraco.Cms.Features.Chatbot.Services;

public interface IChatbotBm25RankingService
{
    IReadOnlyList<ChatbotLexicalRankedResult> FindTop(
        string question,
        int limit,
        ChatbotKnowledgeItemKind? kind = null);
}