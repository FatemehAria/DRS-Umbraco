using DrsUmbraco.Cms.Features.Chatbot.Models;
using DrsUmbraco.Cms.Features.Chatbot.Search;

namespace DrsUmbraco.Cms.Features.Chatbot.Services;

public sealed class ChatbotCandidateEvidenceService
    : IChatbotCandidateEvidenceService
{
    private readonly IChatbotSemanticRankingService
        _semanticRankingService;

    private readonly IChatbotSemanticCentroidRankingService
        _centroidRankingService;

    private readonly IChatbotWeightedLexicalRankingService
        _weightedLexicalRankingService;

    private readonly IChatbotBm25RankingService
        _bm25RankingService;

    public ChatbotCandidateEvidenceService(
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

    public IReadOnlyList<ChatbotCandidateEvidence> Find(
        string question,
        int limit,
        ChatbotKnowledgeItemKind? kind = null)
    {
        if (string.IsNullOrWhiteSpace(question) ||
            limit <= 0)
        {
            return [];
        }

        // IReadOnlyList<ChatbotSemanticRankedResult> semantic =
        // _semanticRankingService.FindTop(
        //     question,
        //     limit,
        //     kind);

        int retrievalDepth = Math.Max(limit, 10);

        IReadOnlyList<ChatbotSemanticRankedResult> semantic =
        _semanticRankingService.FindTop(
            question,
            retrievalDepth,
            kind);
        // IReadOnlyList<ChatbotSemanticRankedResult> centroid =
        //     _centroidRankingService.FindTop(
        //         question,
        //         limit,
        //         kind);
        IReadOnlyList<ChatbotSemanticRankedResult> centroid =
            _centroidRankingService.FindTop(
                question,
                retrievalDepth,
                kind);
        // IReadOnlyList<ChatbotLexicalRankedResult> weightedLexical =
        //     _weightedLexicalRankingService.FindTop(
        //         question,
        //         limit,
        //         kind);
        IReadOnlyList<ChatbotLexicalRankedResult> weightedLexical =
            _weightedLexicalRankingService.FindTop(
                question,
                retrievalDepth,
                kind);

        // IReadOnlyList<ChatbotLexicalRankedResult> bm25 =
        //     _bm25RankingService.FindTop(
        //         question,
        //         limit,
        //         kind);
        IReadOnlyList<ChatbotLexicalRankedResult> bm25 =
            _bm25RankingService.FindTop(
                question,
                retrievalDepth,
                kind);

        HashSet<Guid> knowledgeItemIds =
            semantic
                .Select(x => x.KnowledgeItemId)
                .Concat(
                    centroid.Select(
                        x => x.KnowledgeItemId))
                .Concat(
                    weightedLexical.Select(
                        x => x.KnowledgeItemId))
                .Concat(
                    bm25.Select(
                        x => x.KnowledgeItemId))
                .ToHashSet();

        List<ChatbotCandidateEvidence> results = [];

        foreach (Guid knowledgeItemId in knowledgeItemIds)
        {
            ChatbotSemanticRankedResult? semanticItem =
                semantic.FirstOrDefault(
                    x =>
                        x.KnowledgeItemId ==
                        knowledgeItemId);

            ChatbotSemanticRankedResult? centroidItem =
                centroid.FirstOrDefault(
                    x =>
                        x.KnowledgeItemId ==
                        knowledgeItemId);

            ChatbotLexicalRankedResult? weightedLexicalItem =
                weightedLexical.FirstOrDefault(
                    x =>
                        x.KnowledgeItemId ==
                        knowledgeItemId);

            ChatbotLexicalRankedResult? bm25Item =
                bm25.FirstOrDefault(
                    x =>
                        x.KnowledgeItemId ==
                        knowledgeItemId);

            string? answer =
                semanticItem?.Answer ??
                centroidItem?.Answer ??
                weightedLexicalItem?.Answer ??
                bm25Item?.Answer;

            if (answer is null)
            {
                continue;
            }

            results.Add(
                new ChatbotCandidateEvidence
                {
                    KnowledgeItemId =
                        knowledgeItemId,

                    Answer =
                        answer,

                    SemanticRank =
                        semanticItem?.Rank,

                    SemanticScore =
                        semanticItem?.Score,

                    SemanticMatchedText =
                        semanticItem?.MatchedText,

                    CentroidRank =
                        centroidItem?.Rank,

                    CentroidScore =
                        centroidItem?.Score,

                    WeightedLexicalRank =
                        weightedLexicalItem?.Rank,

                    WeightedLexicalScore =
                        weightedLexicalItem?.Score,

                    WeightedLexicalMatchedText =
                        weightedLexicalItem?.MatchedText,

                    Bm25Rank =
                        bm25Item?.Rank,

                    Bm25Score =
                        bm25Item?.Score,

                    Bm25MatchedText =
                        bm25Item?.MatchedText
                });
        }

        return results
        .OrderByDescending(GetFusionScore)
        .ThenByDescending(GetStrategyCount)
        .ThenBy(GetBestRank)
        .ThenBy(x => x.KnowledgeItemId)
        .ToArray();
    }

    private static int GetBestRank(
        ChatbotCandidateEvidence evidence)
    {
        int[] ranks =
        [
            evidence.SemanticRank ??
                int.MaxValue,

            evidence.CentroidRank ??
                int.MaxValue,

            evidence.WeightedLexicalRank ??
                int.MaxValue,

            evidence.Bm25Rank ??
                int.MaxValue
        ];

        return ranks.Min();
    }

    private static int GetStrategyCount(
        ChatbotCandidateEvidence evidence)
    {
        int count = 0;

        if (evidence.SemanticRank.HasValue)
        {
            count++;
        }

        if (evidence.CentroidRank.HasValue)
        {
            count++;
        }

        if (evidence.WeightedLexicalRank.HasValue)
        {
            count++;
        }

        if (evidence.Bm25Rank.HasValue)
        {
            count++;
        }

        return count;
    }

    private static double GetFusionScore(
    ChatbotCandidateEvidence evidence)
    {
        const double k = 60.0;

        double score = 0;

        if (evidence.SemanticRank.HasValue)
        {
            score +=
                1.0 /
                (k + evidence.SemanticRank.Value);
        }

        if (evidence.CentroidRank.HasValue)
        {
            score +=
                1.0 /
                (k + evidence.CentroidRank.Value);
        }

        if (evidence.WeightedLexicalRank.HasValue)
        {
            score +=
                1.0 /
                (k + evidence.WeightedLexicalRank.Value);
        }

        if (evidence.Bm25Rank.HasValue)
        {
            score +=
                1.0 /
                (k + evidence.Bm25Rank.Value);
        }

        return score;
    }
}