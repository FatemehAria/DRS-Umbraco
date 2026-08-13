using DrsUmbraco.Cms.Features.Chatbot.Embeddings;

namespace DrsUmbraco.Cms.Tests.Features.Chatbot.Embeddings;

public sealed class ChatbotSemanticSearchServiceTests
{
    [Fact]
    public void FindBest_WhenIndexIsEmpty_ShouldReturnNull()
    {
        FakeEmbeddingService embeddingService = new();
        ChatbotSemanticIndex index = new();

        ChatbotSemanticSearchService service =
            new(embeddingService, index);

        ChatbotSemanticSearchResult? result =
            service.FindBest("User question");

        Assert.Null(result);
        Assert.Empty(embeddingService.GeneratedTexts);
    }

    [Fact]
    public void FindBest_ShouldReturnCandidateWithHighestSimilarity()
    {
        FakeEmbeddingService embeddingService =
            new([1f, 0f]);

        ChatbotSemanticIndex index = new();

        Guid passwordFaqId = Guid.NewGuid();
        Guid supportFaqId = Guid.NewGuid();

        ChatbotSemanticCandidate password =
            CreateCandidate(
                passwordFaqId,
                "Password question",
                [1f, 0f]);

        ChatbotSemanticCandidate support =
            CreateCandidate(
                supportFaqId,
                "Support question",
                [0f, 1f]);

        index.Replace([
            password,
            support
        ]);

        ChatbotSemanticSearchService service =
            new(embeddingService, index);

        ChatbotSemanticSearchResult? result =
            service.FindBest("User question");

        Assert.NotNull(result);

        Assert.Equal(
            passwordFaqId,
            result.KnowledgeItemId);

        Assert.Equal(
            "Password question",
            result.MatchedText);

        Assert.InRange(
            result.Score,
            0.9999f,
            1.0001f);
    }

    [Fact]
    public void FindBest_ShouldReturnSecondBestFromDifferentKnowledgeItem()
    {
        FakeEmbeddingService embeddingService =
            new([1f, 0f]);

        ChatbotSemanticIndex index = new();

        Guid passwordFaqId = Guid.NewGuid();
        Guid supportFaqId = Guid.NewGuid();

        ChatbotSemanticCandidate passwordMain =
            CreateCandidate(
                passwordFaqId,
                "Password main",
                [1f, 0f]);

        ChatbotSemanticCandidate passwordAlternative =
            CreateCandidate(
                passwordFaqId,
                "Password alternative",
                [0.9f, 0.1f]);

        ChatbotSemanticCandidate support =
            CreateCandidate(
                supportFaqId,
                "Support",
                [0.8f, 0.6f]);

        index.Replace([
            passwordMain,
            passwordAlternative,
            support
        ]);

        ChatbotSemanticSearchService service =
            new(embeddingService, index);

        ChatbotSemanticSearchResult? result =
            service.FindBest("User question");

        Assert.NotNull(result);

        Assert.Equal(
            passwordFaqId,
            result.KnowledgeItemId);

        Assert.Equal(
            supportFaqId,
            result.SecondBestKnowledgeItemId);

        Assert.InRange(
            result.SecondBestScore!.Value,
            0.7999f,
            0.8001f);
    }

    [Fact]
    public void FindBest_ShouldCalculateMarginBetweenBestAndSecondBestFaq()
    {
        FakeEmbeddingService embeddingService =
            new([1f, 0f]);

        ChatbotSemanticIndex index = new();

        Guid firstFaqId = Guid.NewGuid();
        Guid secondFaqId = Guid.NewGuid();

        index.Replace([
            CreateCandidate(
                firstFaqId,
                "First",
                [1f, 0f]),

            CreateCandidate(
                secondFaqId,
                "Second",
                [0.8f, 0.6f])
        ]);

        ChatbotSemanticSearchService service =
            new(embeddingService, index);

        ChatbotSemanticSearchResult? result =
            service.FindBest("User question");

        Assert.NotNull(result);
        Assert.NotNull(result.Margin);

        Assert.InRange(
            result.Margin.Value,
            0.1999f,
            0.2001f);
    }

    [Fact]
    public void FindBest_ShouldGenerateEmbeddingOnlyOnceForUserQuestion()
    {
        FakeEmbeddingService embeddingService =
            new([1f, 0f]);

        ChatbotSemanticIndex index = new();

        index.Replace([
            CreateCandidate(
                Guid.NewGuid(),
                "Question 1",
                [1f, 0f]),

            CreateCandidate(
                Guid.NewGuid(),
                "Question 2",
                [0f, 1f]),

            CreateCandidate(
                Guid.NewGuid(),
                "Question 3",
                [0.5f, 0.5f])
        ]);

        ChatbotSemanticSearchService service =
            new(embeddingService, index);

        service.FindBest("User question");

        Assert.Single(
            embeddingService.GeneratedTexts);

        Assert.Equal(
            "User question",
            embeddingService.GeneratedTexts[0]);
    }

    private static ChatbotSemanticCandidate CreateCandidate(
        Guid knowledgeItemId,
        string text,
        float[] embedding)
    {
        return new ChatbotSemanticCandidate
        {
            KnowledgeItemId = knowledgeItemId,
            Text = text,
            Answer = $"Answer for {text}",
            Embedding = embedding
        };
    }

    private sealed class FakeEmbeddingService
        : IEmbeddingService
    {
        private readonly float[] _embedding;

        public FakeEmbeddingService()
            : this([1f, 0f])
        {
        }

        public FakeEmbeddingService(
            float[] embedding)
        {
            _embedding = embedding;
        }

        public List<string> GeneratedTexts { get; } = [];

        public float[] Generate(string text)
        {
            GeneratedTexts.Add(text);

            return _embedding;
        }
    }
}