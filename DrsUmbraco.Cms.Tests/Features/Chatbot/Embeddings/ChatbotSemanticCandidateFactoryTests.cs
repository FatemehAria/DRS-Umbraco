using DrsUmbraco.Cms.Features.Chatbot.Embeddings;
using DrsUmbraco.Cms.Features.Chatbot.Models;

namespace DrsUmbraco.Cms.Tests.Features.Chatbot.Embeddings;

public sealed class ChatbotSemanticCandidateFactoryTests
{
    [Fact]
    public void CreateCandidates_ShouldCreateCandidateForQuestionAndAlternatives()
    {
        FakeEmbeddingService embeddingService = new();

        ChatbotSemanticCandidateFactory factory =
            new(embeddingService);

        Guid itemId = Guid.NewGuid();

        ChatbotKnowledgeItem item =
            new()
            {
                Id = itemId,
                Question = "Main question",
                Answer = "Test answer",
                AlternativeQuestions =
                [
                    "Alternative 1",
                    "Alternative 2"
                ]
            };

        IReadOnlyList<ChatbotSemanticCandidate> result =
            factory.CreateCandidates(item);

        Assert.Equal(3, result.Count);

        Assert.Contains(
            result,
            candidate =>
                candidate.Text == "Main question");

        Assert.Contains(
            result,
            candidate =>
                candidate.Text == "Alternative 1");

        Assert.Contains(
            result,
            candidate =>
                candidate.Text == "Alternative 2");

        Assert.All(
            result,
            candidate =>
            {
                Assert.Equal(itemId, candidate.KnowledgeItemId);
                Assert.Equal("Test answer", candidate.Answer);
            });
    }

    [Fact]
    public void CreateCandidates_ShouldIgnoreEmptyAlternatives()
    {
        FakeEmbeddingService embeddingService = new();

        ChatbotSemanticCandidateFactory factory =
            new(embeddingService);

        ChatbotKnowledgeItem item =
            new()
            {
                Id = Guid.NewGuid(),
                Question = "Main question",
                Answer = "Test answer",
                AlternativeQuestions =
                [
                    "",
                    "   ",
                    "Alternative"
                ]
            };

        IReadOnlyList<ChatbotSemanticCandidate> result =
            factory.CreateCandidates(item);

        Assert.Equal(2, result.Count);

        Assert.Contains(
            result,
            candidate =>
                candidate.Text == "Main question");

        Assert.Contains(
            result,
            candidate =>
                candidate.Text == "Alternative");
    }

    [Fact]
    public void CreateCandidates_ShouldRemoveDuplicateTexts()
    {
        FakeEmbeddingService embeddingService = new();

        ChatbotSemanticCandidateFactory factory =
            new(embeddingService);

        ChatbotKnowledgeItem item =
            new()
            {
                Id = Guid.NewGuid(),
                Question = "Same question",
                Answer = "Test answer",
                AlternativeQuestions =
                [
                    "Same question",
                    "Same question"
                ]
            };

        IReadOnlyList<ChatbotSemanticCandidate> result =
            factory.CreateCandidates(item);

        Assert.Single(result);
        Assert.Equal(
            "Same question",
            result[0].Text);
    }

    [Fact]
    public void CreateCandidates_ShouldGenerateEmbeddingForEachCandidate()
    {
        FakeEmbeddingService embeddingService = new();

        ChatbotSemanticCandidateFactory factory =
            new(embeddingService);

        ChatbotKnowledgeItem item =
            new()
            {
                Id = Guid.NewGuid(),
                Question = "Main question",
                Answer = "Test answer",
                AlternativeQuestions =
                [
                    "Alternative 1",
                    "Alternative 2"
                ]
            };

        IReadOnlyList<ChatbotSemanticCandidate> result =
            factory.CreateCandidates(item);

        Assert.Equal(3, embeddingService.GeneratedTexts.Count);

        Assert.Contains(
            "Main question",
            embeddingService.GeneratedTexts);

        Assert.Contains(
            "Alternative 1",
            embeddingService.GeneratedTexts);

        Assert.Contains(
            "Alternative 2",
            embeddingService.GeneratedTexts);

        Assert.All(
            result,
            candidate =>
                Assert.Equal(
                    [1f, 2f, 3f],
                    candidate.Embedding));
    }

    private sealed class FakeEmbeddingService
        : IEmbeddingService
    {
        public List<string> GeneratedTexts { get; } = [];

        public float[] Generate(string text)
        {
            GeneratedTexts.Add(text);

            return [1f, 2f, 3f];
        }
    }
}