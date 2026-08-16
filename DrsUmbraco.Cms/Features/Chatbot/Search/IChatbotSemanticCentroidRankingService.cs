using DrsUmbraco.Cms.Features.Chatbot.Models;

public interface IChatbotSemanticCentroidRankingService
{
    IReadOnlyList<ChatbotSemanticRankedResult> FindTop(
        string question,
        int limit);
}