using DrsUmbraco.Cms.Features.Chatbot.Models;
using DrsUmbraco.Cms.Features.Chatbot.Services;

public sealed class ChatbotRerankingService
    : IChatbotRerankingService
{
    private readonly IChatbotSemanticRankingService _semanticRankingService;

    private readonly IChatbotSemanticCentroidRankingService _centroidRankingService;

    public ChatbotRerankingService(
        IChatbotSemanticRankingService semanticRankingService,
        IChatbotSemanticCentroidRankingService centroidRankingService)
    {
        _semanticRankingService = semanticRankingService;
        _centroidRankingService = centroidRankingService;
    }

    public ChatbotRerankResult? FindBest(
        string question,
        ChatbotKnowledgeItemKind? kind = null)
    {
        if (string.IsNullOrWhiteSpace(question))
        {
            return null;
        }

        IReadOnlyList<ChatbotSemanticRankedResult> semantic =
            _semanticRankingService.FindTop(
                question,
                limit: 2,
                kind);

        IReadOnlyList<ChatbotSemanticRankedResult> centroid =
            _centroidRankingService.FindTop(
                question,
                limit: 2,
                kind);

        if (semantic.Count == 0 ||
            centroid.Count == 0)
        {
            return null;
        }

        ChatbotSemanticRankedResult semanticTop = semantic[0];

        ChatbotSemanticRankedResult centroidTop = centroid[0];

        float semanticMargin = CalculateMargin(semantic);

        float centroidMargin = CalculateMargin(centroid);

        ChatbotSemanticRankedResult selected;
        string selectedStrategy;

        if (semanticTop.KnowledgeItemId ==
            centroidTop.KnowledgeItemId)
        {
            selected = semanticTop;
            selectedStrategy = "Agreement";
        }
        else if (semanticMargin >= centroidMargin)
        {
            selected = semanticTop;
            selectedStrategy = "Semantic";
        }
        else
        {
            selected = centroidTop;
            selectedStrategy = "Centroid";
        }

        return new ChatbotRerankResult
        {
            KnowledgeItemId = selected.KnowledgeItemId,

            Answer = selected.Answer,

            SelectedStrategy = selectedStrategy,

            SemanticTopScore = semanticTop.Score,

            SemanticMargin = semanticMargin,

            CentroidTopScore = centroidTop.Score,

            CentroidMargin = centroidMargin
        };
    }

    private static float CalculateMargin(
        IReadOnlyList<ChatbotSemanticRankedResult> results)
    {
        if (results.Count < 2)
        {
            return 1f;
        }

        return results[0].Score - results[1].Score;
    }
}