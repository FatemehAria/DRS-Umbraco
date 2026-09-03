using DrsUmbraco.Cms.Features.Chatbot.Models;
using DrsUmbraco.Cms.Features.Chatbot.Search;
using System.Diagnostics;

namespace DrsUmbraco.Cms.Features.Chatbot.Services;

public sealed class ChatbotCandidateEvidenceService
    : IChatbotCandidateEvidenceService
{
    private readonly IChatbotSemanticRankingService _semanticRankingService;

    private readonly IChatbotSemanticCentroidRankingService _centroidRankingService;

    private readonly IChatbotWeightedLexicalRankingService _weightedLexicalRankingService;

    private readonly IChatbotBm25RankingService _bm25RankingService;

    private readonly ILogger<ChatbotCandidateEvidenceService> _logger;
    public ChatbotCandidateEvidenceService(
        IChatbotSemanticRankingService semanticRankingService,
        IChatbotSemanticCentroidRankingService centroidRankingService,
        IChatbotWeightedLexicalRankingService weightedLexicalRankingService,
        IChatbotBm25RankingService bm25RankingService,
        ILogger<ChatbotCandidateEvidenceService> logger)
    {
        _semanticRankingService = semanticRankingService;

        _centroidRankingService = centroidRankingService;

        _weightedLexicalRankingService = weightedLexicalRankingService;

        _bm25RankingService = bm25RankingService;

        _logger = logger;
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

        Stopwatch semanticStopwatch = Stopwatch.StartNew();

        IReadOnlyList<ChatbotSemanticRankedResult> semantic =
        _semanticRankingService.FindTop(
            question,
            retrievalDepth,
            kind);

        semanticStopwatch.Stop();

        // IReadOnlyList<ChatbotSemanticRankedResult> centroid =
        //     _centroidRankingService.FindTop(
        //         question,
        //         limit,
        //         kind);

        Stopwatch centroidStopwatch = Stopwatch.StartNew();

        IReadOnlyList<ChatbotSemanticRankedResult> centroid =
            _centroidRankingService.FindTop(
                question,
                retrievalDepth,
                kind);

        centroidStopwatch.Stop();
        // IReadOnlyList<ChatbotLexicalRankedResult> weightedLexical =
        //     _weightedLexicalRankingService.FindTop(
        //         question,
        //         limit,
        //         kind);

        Stopwatch weightedLexicalStopwatch = Stopwatch.StartNew();

        IReadOnlyList<ChatbotLexicalRankedResult> weightedLexical =
            _weightedLexicalRankingService.FindTop(
                question,
                retrievalDepth,
                kind);

        weightedLexicalStopwatch.Stop();
        // IReadOnlyList<ChatbotLexicalRankedResult> bm25 =
        //     _bm25RankingService.FindTop(
        //         question,
        //         limit,
        //         kind);

        Stopwatch bm25Stopwatch = Stopwatch.StartNew();

        IReadOnlyList<ChatbotLexicalRankedResult> bm25 =
            _bm25RankingService.FindTop(
                question,
                retrievalDepth,
                kind);

        bm25Stopwatch.Stop();

        Stopwatch evidenceAssemblyStopwatch = Stopwatch.StartNew();

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

        evidenceAssemblyStopwatch.Stop();

        Stopwatch sortingStopwatch =
            Stopwatch.StartNew();

        ChatbotCandidateEvidence[] orderedResults =
            results
                .OrderByDescending(GetFusionScore)
                .ThenByDescending(GetStrategyCount)
                .ThenBy(GetBestRank)
                .ThenBy(x => x.KnowledgeItemId)
                .ToArray();

        sortingStopwatch.Stop();

        _logger.LogInformation(
            "Performance metric {MetricName}. " +
            "RetrievalDepth={RetrievalDepth}, FinalCandidateCount={FinalCandidateCount}. " +
            "SemanticMs={SemanticMs}, SemanticCount={SemanticCount}. " +
            "CentroidMs={CentroidMs}, CentroidCount={CentroidCount}. " +
            "WeightedLexicalMs={WeightedLexicalMs}, WeightedLexicalCount={WeightedLexicalCount}. " +
            "Bm25Ms={Bm25Ms}, Bm25Count={Bm25Count}. " +
            "EvidenceAssemblyMs={EvidenceAssemblyMs}, SortingMs={SortingMs}.",
            "CandidateEvidenceBreakdown",
            retrievalDepth,
            orderedResults.Length,
            semanticStopwatch.Elapsed.TotalMilliseconds,
            semantic.Count,
            centroidStopwatch.Elapsed.TotalMilliseconds,
            centroid.Count,
            weightedLexicalStopwatch.Elapsed.TotalMilliseconds,
            weightedLexical.Count,
            bm25Stopwatch.Elapsed.TotalMilliseconds,
            bm25.Count,
            evidenceAssemblyStopwatch.Elapsed.TotalMilliseconds,
            sortingStopwatch.Elapsed.TotalMilliseconds);

        return orderedResults;
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