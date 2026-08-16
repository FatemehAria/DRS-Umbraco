using DrsUmbraco.Cms.Features.Chatbot.Models;
using DrsUmbraco.Cms.Features.Chatbot.Search;

namespace DrsUmbraco.Cms.Features.Chatbot.Services;

public sealed class ChatbotAmbiguityEvidenceService
    : IChatbotAmbiguityEvidenceService
{
    private readonly IChatbotSemanticRankingService
        _semanticRankingService;

    private readonly IChatbotSemanticCentroidRankingService
        _centroidRankingService;

    private readonly IChatbotWeightedLexicalRankingService
        _weightedLexicalRankingService;

    private readonly IChatbotBm25RankingService
        _bm25RankingService;

    public ChatbotAmbiguityEvidenceService(
        IChatbotSemanticRankingService semanticRankingService,
        IChatbotSemanticCentroidRankingService centroidRankingService,
        IChatbotWeightedLexicalRankingService weightedLexicalRankingService,
        IChatbotBm25RankingService bm25RankingService)
    {
        _semanticRankingService =
            semanticRankingService;

        _centroidRankingService =
            centroidRankingService;

        _weightedLexicalRankingService =
            weightedLexicalRankingService;

        _bm25RankingService =
            bm25RankingService;
    }

    public ChatbotAmbiguityEvidence? Analyze(
        string question)
    {
        if (string.IsNullOrWhiteSpace(question))
        {
            return null;
        }

        IReadOnlyList<ChatbotSemanticRankedResult> semantic =
            _semanticRankingService.FindTop(
                question,
                2);

        IReadOnlyList<ChatbotSemanticRankedResult> centroid =
            _centroidRankingService.FindTop(
                question,
                2);

        IReadOnlyList<ChatbotLexicalRankedResult> weighted =
            _weightedLexicalRankingService.FindTop(
                question,
                2);

        IReadOnlyList<ChatbotLexicalRankedResult> bm25 =
            _bm25RankingService.FindTop(
                question,
                2);

        if (semantic.Count == 0)
        {
            return null;
        }

        ChatbotSemanticRankedResult semanticTop =
            semantic[0];

        ChatbotSemanticRankedResult? centroidTop =
            centroid.FirstOrDefault();

        ChatbotLexicalRankedResult? weightedTop =
            weighted.FirstOrDefault();

        ChatbotLexicalRankedResult? bm25Top =
            bm25.FirstOrDefault();

        float? semanticMargin =
            CalculateMargin(semantic);

        float? centroidMargin =
            CalculateMargin(centroid);

        List<Guid> topIds =
        [
            semanticTop.KnowledgeItemId
        ];

        if (centroidTop is not null)
        {
            topIds.Add(
                centroidTop.KnowledgeItemId);
        }

        if (weightedTop is not null)
        {
            topIds.Add(
                weightedTop.KnowledgeItemId);
        }

        if (bm25Top is not null)
        {
            topIds.Add(
                bm25Top.KnowledgeItemId);
        }

        int top1AgreementCount =
            topIds
                .GroupBy(id => id)
                .Max(group => group.Count());

        float? weightedLexicalMargin =
            CalculateLexicalMargin(weighted);

        float? bm25Margin =
            CalculateLexicalMargin(bm25);

        return new ChatbotAmbiguityEvidence
        {
            SemanticTopKnowledgeItemId =
         semanticTop.KnowledgeItemId,

            SemanticTopScore =
         semanticTop.Score,

            SemanticMargin =
         semanticMargin,

            CentroidTopKnowledgeItemId =
         centroidTop?.KnowledgeItemId,

            CentroidTopScore =
         centroidTop?.Score,

            CentroidMargin =
         centroidMargin,

            WeightedLexicalTopKnowledgeItemId =
         weightedTop?.KnowledgeItemId,

            WeightedLexicalTopScore =
         weightedTop?.Score,

            WeightedLexicalMargin =
         weightedLexicalMargin,

            Bm25TopKnowledgeItemId =
         bm25Top?.KnowledgeItemId,

            Bm25TopScore =
         bm25Top?.Score,

            Bm25Margin =
         bm25Margin,

            SemanticCentroidAgree =
         centroidTop is not null &&
         semanticTop.KnowledgeItemId ==
         centroidTop.KnowledgeItemId,

            Top1AgreementCount =
         top1AgreementCount,

            ActiveStrategyCount =
         topIds.Count
        };
    }

    private static float? CalculateMargin(
        IReadOnlyList<ChatbotSemanticRankedResult> results)
    {
        if (results.Count < 2)
        {
            return null;
        }

        return results[0].Score -
               results[1].Score;
    }

    private static float? CalculateLexicalMargin(
    IReadOnlyList<ChatbotLexicalRankedResult> results)
    {
        if (results.Count < 2)
        {
            return null;
        }

        return results[0].Score -
               results[1].Score;
    }
}