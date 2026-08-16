using DrsUmbraco.Cms.Features.Chatbot.Models;
using DrsUmbraco.Cms.Features.Chatbot.Services;

namespace DrsUmbraco.Cms.Tests.Features.Chatbot.Services;

public sealed class ChatbotRerankingServiceTests
{
    [Fact]
    public void FindBest_WhenBothStrategiesAgree_ShouldSelectAgreement()
    {
        Guid expectedId = Guid.NewGuid();

        FakeSemanticRankingService semanticService =
            new(
                [
                    CreateResult(
                        rank: 1,
                        knowledgeItemId: expectedId,
                        score: 0.92f),

                    CreateResult(
                        rank: 2,
                        knowledgeItemId: Guid.NewGuid(),
                        score: 0.90f)
                ]);

        FakeCentroidRankingService centroidService =
            new(
                [
                    CreateResult(
                        rank: 1,
                        knowledgeItemId: expectedId,
                        score: 0.91f),

                    CreateResult(
                        rank: 2,
                        knowledgeItemId: Guid.NewGuid(),
                        score: 0.89f)
                ]);

        ChatbotRerankingService service =
            new(
                semanticService,
                centroidService);

        ChatbotRerankResult? result =
            service.FindBest("test question");

        Assert.NotNull(result);

        Assert.Equal(
            expectedId,
            result.KnowledgeItemId);

        Assert.Equal(
            "Agreement",
            result.SelectedStrategy);
    }

    [Fact]
    public void FindBest_WhenSemanticMarginIsLarger_ShouldSelectSemantic()
    {
        Guid semanticId = Guid.NewGuid();
        Guid centroidId = Guid.NewGuid();

        FakeSemanticRankingService semanticService =
            new(
                [
                    CreateResult(
                        rank: 1,
                        knowledgeItemId: semanticId,
                        score: 0.95f),

                    CreateResult(
                        rank: 2,
                        knowledgeItemId: Guid.NewGuid(),
                        score: 0.85f)
                ]);

        FakeCentroidRankingService centroidService =
            new(
                [
                    CreateResult(
                        rank: 1,
                        knowledgeItemId: centroidId,
                        score: 0.93f),

                    CreateResult(
                        rank: 2,
                        knowledgeItemId: Guid.NewGuid(),
                        score: 0.90f)
                ]);

        ChatbotRerankingService service =
            new(
                semanticService,
                centroidService);

        ChatbotRerankResult? result =
            service.FindBest("test question");

        Assert.NotNull(result);

        Assert.Equal(
            semanticId,
            result.KnowledgeItemId);

        Assert.Equal(
            "Semantic",
            result.SelectedStrategy);

        Assert.True(
            result.SemanticMargin >
            result.CentroidMargin);
    }

    [Fact]
    public void FindBest_WhenCentroidMarginIsLarger_ShouldSelectCentroid()
    {
        Guid semanticId = Guid.NewGuid();
        Guid centroidId = Guid.NewGuid();

        FakeSemanticRankingService semanticService =
            new(
                [
                    CreateResult(
                        rank: 1,
                        knowledgeItemId: semanticId,
                        score: 0.92f),

                    CreateResult(
                        rank: 2,
                        knowledgeItemId: Guid.NewGuid(),
                        score: 0.91f)
                ]);

        FakeCentroidRankingService centroidService =
            new(
                [
                    CreateResult(
                        rank: 1,
                        knowledgeItemId: centroidId,
                        score: 0.95f),

                    CreateResult(
                        rank: 2,
                        knowledgeItemId: Guid.NewGuid(),
                        score: 0.85f)
                ]);

        ChatbotRerankingService service =
            new(
                semanticService,
                centroidService);

        ChatbotRerankResult? result =
            service.FindBest("test question");

        Assert.NotNull(result);

        Assert.Equal(
            centroidId,
            result.KnowledgeItemId);

        Assert.Equal(
            "Centroid",
            result.SelectedStrategy);

        Assert.True(
            result.CentroidMargin >
            result.SemanticMargin);
    }

    [Fact]
    public void FindBest_WhenQuestionIsWhitespace_ShouldReturnNull()
    {
        FakeSemanticRankingService semanticService =
            new([]);

        FakeCentroidRankingService centroidService =
            new([]);

        ChatbotRerankingService service =
            new(
                semanticService,
                centroidService);

        ChatbotRerankResult? result =
            service.FindBest("   ");

        Assert.Null(result);
    }

    private static ChatbotSemanticRankedResult CreateResult(
        int rank,
        Guid knowledgeItemId,
        float score)
    {
        return new ChatbotSemanticRankedResult
        {
            Rank = rank,
            KnowledgeItemId = knowledgeItemId,
            MatchedText = "Test text",
            Answer = $"Answer {knowledgeItemId}",
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
                int limit)
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
                int limit)
        {
            return _results
                .Take(limit)
                .ToArray();
        }
    }
}