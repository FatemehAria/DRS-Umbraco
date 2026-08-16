using DrsUmbraco.Cms.Features.Chatbot.Embeddings;
using DrsUmbraco.Cms.Features.Chatbot.Models;
using DrsUmbraco.Cms.Features.Chatbot.Search;
using DrsUmbraco.Cms.Features.Chatbot.Services;

namespace DrsUmbraco.Cms.Tests.Features.Chatbot.Search;

public sealed class ChatbotHybridSearchServiceTests
{
    [Fact]
    public void FindBest_ShouldCombineSemanticAndLexicalScores()
    {
        FakeEmbeddingService embeddingService =
            new([1f, 0f]);

        ChatbotSemanticIndex index = new();

        Guid firstFaqId = Guid.NewGuid();
        Guid secondFaqId = Guid.NewGuid();

        index.Replace([
            CreateCandidate(
                firstFaqId,
                "First candidate",
                [0.95f, 0.3122499f]),

            CreateCandidate(
                secondFaqId,
                "Second candidate",
                [0.85f, 0.5267827f])
        ]);

        FakeLexicalSimilarityCalculator lexicalCalculator =
            new(new Dictionary<string, float>
            {
                ["First candidate"] = 0f,
                ["Second candidate"] = 1f
            });

        ChatbotHybridSearchService service =
            new(
                embeddingService,
                index,
                lexicalCalculator);

        ChatbotHybridSearchResult? result =
            service.FindBest("User question");

        Assert.NotNull(result);

        Assert.Equal(
            secondFaqId,
            result.KnowledgeItemId);

        Assert.Equal(
            "Second candidate",
            result.MatchedText);
    }

    [Fact]
    public void FindBest_ShouldUseSecondBestFromDifferentKnowledgeItem()
    {
        FakeEmbeddingService embeddingService =
            new([1f, 0f]);

        ChatbotSemanticIndex index = new();

        Guid passwordFaqId = Guid.NewGuid();
        Guid supportFaqId = Guid.NewGuid();

        index.Replace([
            CreateCandidate(
                passwordFaqId,
                "Password main",
                [1f, 0f]),

            CreateCandidate(
                passwordFaqId,
                "Password alternative",
                [0.99f, 0.1410674f]),

            CreateCandidate(
                supportFaqId,
                "Support",
                [0.8f, 0.6f])
        ]);

        FakeLexicalSimilarityCalculator lexicalCalculator =
            new(new Dictionary<string, float>
            {
                ["Password main"] = 0.8f,
                ["Password alternative"] = 0.7f,
                ["Support"] = 0.5f
            });

        ChatbotHybridSearchService service =
            new(
                embeddingService,
                index,
                lexicalCalculator);

        ChatbotHybridSearchResult? result =
            service.FindBest("User question");

        Assert.NotNull(result);

        Assert.Equal(
            passwordFaqId,
            result.KnowledgeItemId);

        Assert.Equal(
            supportFaqId,
            result.SecondBestKnowledgeItemId);
    }

    [Fact]
    public void FindBest_ShouldGenerateUserEmbeddingOnlyOnce()
    {
        FakeEmbeddingService embeddingService =
            new([1f, 0f]);

        ChatbotSemanticIndex index = new();

        index.Replace([
            CreateCandidate(
                Guid.NewGuid(),
                "Candidate 1",
                [1f, 0f]),

            CreateCandidate(
                Guid.NewGuid(),
                "Candidate 2",
                [0.8f, 0.6f]),

            CreateCandidate(
                Guid.NewGuid(),
                "Candidate 3",
                [0.6f, 0.8f])
        ]);

        FakeLexicalSimilarityCalculator lexicalCalculator = new(new Dictionary<string, float>());
        
        ChatbotHybridSearchService service =
            new(
                embeddingService,
                index,
                lexicalCalculator);

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
            Embedding = embedding,
            Kind = ChatbotKnowledgeItemKind.Clarification
        };
    }

    private sealed class FakeEmbeddingService
        : IEmbeddingService
    {
        private readonly float[] _embedding;

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

    private sealed class FakeLexicalSimilarityCalculator
        : ILexicalSimilarityCalculator
    {
        private readonly IReadOnlyDictionary<string, float> _scores;

        public FakeLexicalSimilarityCalculator(
            IReadOnlyDictionary<string, float> scores)
        {
            _scores = scores;
        }

        public float Calculate(
            string? firstText,
            string? secondText)
        {
            if (secondText is null)
            {
                return 0f;
            }

            return _scores.TryGetValue(
                secondText,
                out float score)
                ? score
                : 0f;
        }
    }
}