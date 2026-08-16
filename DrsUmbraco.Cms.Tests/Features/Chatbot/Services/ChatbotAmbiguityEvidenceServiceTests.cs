using DrsUmbraco.Cms.Features.Chatbot.Services;
using DrsUmbraco.Cms.Features.Chatbot.Search;
using DrsUmbraco.Cms.Features.Chatbot.Models;

namespace DrsUmbraco.Cms.Tests.Features.Chatbot.Services;

public sealed class ChatbotAmbiguityEvidenceServiceTests
{
    [Fact]
    public void Analyze_WhenThreeStrategiesAgree_ShouldReturnCorrectEvidence()
    {
        Guid knowledgeItemA = Guid.NewGuid();
        Guid knowledgeItemB = Guid.NewGuid();
        Guid knowledgeItemC = Guid.NewGuid();

        FakeSemanticRankingService semantic =
            new(
                [
                    CreateSemanticResult(
                        rank: 1,
                        id: knowledgeItemA,
                        score: 0.93f),

                    CreateSemanticResult(
                        rank: 2,
                        id: knowledgeItemB,
                        score: 0.90f)
                ]);

        FakeCentroidRankingService centroid =
            new(
                [
                    CreateSemanticResult(
                        rank: 1,
                        id: knowledgeItemA,
                        score: 0.91f),

                    CreateSemanticResult(
                        rank: 2,
                        id: knowledgeItemC,
                        score: 0.89f)
                ]);

        FakeWeightedLexicalRankingService weighted =
            new(
                [
                    CreateLexicalResult(
                        rank: 1,
                        id: knowledgeItemB,
                        score: 0.30f)
                ]);

        FakeBm25RankingService bm25 =
            new(
                [
                    CreateLexicalResult(
                        rank: 1,
                        id: knowledgeItemA,
                        score: 4.2f)
                ]);

        ChatbotAmbiguityEvidenceService service =
            new(
                semantic,
                centroid,
                weighted,
                bm25);

        ChatbotAmbiguityEvidence? result =
            service.Analyze("test question");

        Assert.NotNull(result);

        Assert.Equal(
            knowledgeItemA,
            result.SemanticTopKnowledgeItemId);

        Assert.Equal(
            knowledgeItemA,
            result.CentroidTopKnowledgeItemId);

        Assert.Equal(
            knowledgeItemB,
            result.WeightedLexicalTopKnowledgeItemId);

        Assert.Equal(
            knowledgeItemA,
            result.Bm25TopKnowledgeItemId);

        Assert.True(
            result.SemanticCentroidAgree);

        Assert.Equal(
            3,
            result.Top1AgreementCount);

        Assert.Equal(
            4,
            result.ActiveStrategyCount);

        Assert.NotNull(result.SemanticMargin);

        Assert.Equal(
            0.03f,
            result.SemanticMargin.Value,
            3);

        Assert.NotNull(result.CentroidMargin);

        Assert.Equal(
            0.02f,
            result.CentroidMargin.Value,
            3);
    }

    [Fact]
    public void Analyze_WhenBm25HasNoResult_ShouldCountOnlyActiveStrategies()
    {
        Guid knowledgeItemA = Guid.NewGuid();
        Guid knowledgeItemB = Guid.NewGuid();

        FakeSemanticRankingService semantic =
            new(
                [
                    CreateSemanticResult(
                    rank: 1,
                    id: knowledgeItemA,
                    score: 0.93f),

                CreateSemanticResult(
                    rank: 2,
                    id: knowledgeItemB,
                    score: 0.90f)
                ]);

        FakeCentroidRankingService centroid =
            new(
                [
                    CreateSemanticResult(
                    rank: 1,
                    id: knowledgeItemA,
                    score: 0.92f),

                CreateSemanticResult(
                    rank: 2,
                    id: knowledgeItemB,
                    score: 0.89f)
                ]);

        FakeWeightedLexicalRankingService weighted =
            new(
                [
                    CreateLexicalResult(
                    rank: 1,
                    id: knowledgeItemA,
                    score: 0.30f)
                ]);

        FakeBm25RankingService bm25 =
            new([]);

        ChatbotAmbiguityEvidenceService service =
            new(
                semantic,
                centroid,
                weighted,
                bm25);

        ChatbotAmbiguityEvidence? result =
            service.Analyze("test question");

        Assert.NotNull(result);

        Assert.Null(
            result.Bm25TopKnowledgeItemId);

        Assert.Equal(
            3,
            result.ActiveStrategyCount);

        Assert.Equal(
            3,
            result.Top1AgreementCount);

        Assert.True(
            result.SemanticCentroidAgree);
    }
    
    private static ChatbotSemanticRankedResult
        CreateSemanticResult(
            int rank,
            Guid id,
            float score)
    {
        return new ChatbotSemanticRankedResult
        {
            Rank = rank,
            KnowledgeItemId = id,
            Answer = "answer",
            MatchedText = "text",
            Score = score
        };
    }

    private static ChatbotLexicalRankedResult
        CreateLexicalResult(
            int rank,
            Guid id,
            float score)
    {
        return new ChatbotLexicalRankedResult
        {
            Rank = rank,
            KnowledgeItemId = id,
            Answer = "answer",
            MatchedText = "text",
            Score = score
        };
    }

    private sealed class FakeSemanticRankingService
    : IChatbotSemanticRankingService
    {
        private readonly IReadOnlyList<
            ChatbotSemanticRankedResult> _results;

        public FakeSemanticRankingService(
            IReadOnlyList<
                ChatbotSemanticRankedResult> results)
        {
            _results = results;
        }

        public IReadOnlyList<
            ChatbotSemanticRankedResult> FindTop(
                string question,
                int limit,
                ChatbotKnowledgeItemKind? kind = null)
        {
            return _results
                .Take(limit)
                .ToArray();
        }
    }

    private sealed class FakeCentroidRankingService
        : IChatbotSemanticCentroidRankingService
    {
        private readonly IReadOnlyList<
            ChatbotSemanticRankedResult> _results;

        public FakeCentroidRankingService(
            IReadOnlyList<
                ChatbotSemanticRankedResult> results)
        {
            _results = results;
        }

        public IReadOnlyList<
            ChatbotSemanticRankedResult> FindTop(
                string question,
                int limit,
                ChatbotKnowledgeItemKind? kind = null)
        {
            return _results
                .Take(limit)
                .ToArray();
        }
    }

    private sealed class FakeWeightedLexicalRankingService
        : IChatbotWeightedLexicalRankingService
    {
        private readonly IReadOnlyList<
            ChatbotLexicalRankedResult> _results;

        public FakeWeightedLexicalRankingService(
            IReadOnlyList<
                ChatbotLexicalRankedResult> results)
        {
            _results = results;
        }

        public IReadOnlyList<
            ChatbotLexicalRankedResult> FindTop(
                string question,
                int limit,
                ChatbotKnowledgeItemKind? kind = null)
        {
            return _results
                .Take(limit)
                .ToArray();
        }
    }

    private sealed class FakeBm25RankingService
        : IChatbotBm25RankingService
    {
        private readonly IReadOnlyList<
            ChatbotLexicalRankedResult> _results;

        public FakeBm25RankingService(
            IReadOnlyList<
                ChatbotLexicalRankedResult> results)
        {
            _results = results;
        }

        public IReadOnlyList<
            ChatbotLexicalRankedResult> FindTop(
                string question,
                int limit,
                ChatbotKnowledgeItemKind? kind = null)
        {
            return _results
                .Take(limit)
                .ToArray();
        }
    }
}