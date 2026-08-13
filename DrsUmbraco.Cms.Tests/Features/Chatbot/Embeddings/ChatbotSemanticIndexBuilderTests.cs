using DrsUmbraco.Cms.Features.Chatbot.Embeddings;
using DrsUmbraco.Cms.Features.Chatbot.Models;
using DrsUmbraco.Cms.Features.Chatbot.Services;

namespace DrsUmbraco.Cms.Tests.Features.Chatbot.Embeddings;

public sealed class ChatbotSemanticIndexBuilderTests
{
    [Fact]
    public void Rebuild_ShouldBuildCandidatesForAllKnowledgeItems()
    {
        ChatbotKnowledgeItem firstItem =
            CreateKnowledgeItem("Question 1");

        ChatbotKnowledgeItem secondItem =
            CreateKnowledgeItem("Question 2");

        FakeKnowledgeService knowledgeService =
            new([firstItem, secondItem]);

        FakeCandidateFactory candidateFactory = new();

        ChatbotSemanticIndex semanticIndex = new();

        ChatbotSemanticIndexBuilder builder =
            new(
                knowledgeService,
                candidateFactory,
                semanticIndex);

        int result = builder.Rebuild();

        Assert.Equal(2, result);

        Assert.Equal(
            2,
            candidateFactory.CreatedForItems.Count);

        Assert.Contains(
            firstItem.Id,
            candidateFactory.CreatedForItems);

        Assert.Contains(
            secondItem.Id,
            candidateFactory.CreatedForItems);

        Assert.Equal(
            2,
            semanticIndex.GetAll().Count);
    }

    [Fact]
    public void Rebuild_ShouldReplaceOldIndexContents()
    {
        ChatbotSemanticIndex semanticIndex = new();

        semanticIndex.Replace([
            new ChatbotSemanticCandidate
            {
                KnowledgeItemId = Guid.NewGuid(),
                Text = "Old question",
                Answer = "Old answer",
                Embedding = [1f, 0f]
            }
        ]);

        ChatbotKnowledgeItem newItem =
            CreateKnowledgeItem("New question");

        FakeKnowledgeService knowledgeService =
            new([newItem]);

        FakeCandidateFactory candidateFactory = new();

        ChatbotSemanticIndexBuilder builder =
            new(
                knowledgeService,
                candidateFactory,
                semanticIndex);

        builder.Rebuild();

        IReadOnlyList<ChatbotSemanticCandidate> candidates =
            semanticIndex.GetAll();

        Assert.Single(candidates);

        Assert.Equal(
            newItem.Id,
            candidates[0].KnowledgeItemId);

        Assert.Equal(
            "New question",
            candidates[0].Text);
    }

    private static ChatbotKnowledgeItem CreateKnowledgeItem(
        string question)
    {
        return new ChatbotKnowledgeItem
        {
            Id = Guid.NewGuid(),
            Question = question,
            Answer = $"Answer for {question}"
        };
    }

    private sealed class FakeKnowledgeService
        : IChatbotKnowledgeService
    {
        private readonly IReadOnlyList<ChatbotKnowledgeItem> _items;

        public FakeKnowledgeService(
            IReadOnlyList<ChatbotKnowledgeItem> items)
        {
            _items = items;
        }

        public IReadOnlyList<ChatbotKnowledgeItem> GetAll()
        {
            return _items;
        }
    }

    private sealed class FakeCandidateFactory
        : IChatbotSemanticCandidateFactory
    {
        public List<Guid> CreatedForItems { get; } = [];

        public IReadOnlyList<ChatbotSemanticCandidate>
            CreateCandidates(ChatbotKnowledgeItem item)
        {
            CreatedForItems.Add(item.Id);

            return
            [
                new ChatbotSemanticCandidate
                {
                    KnowledgeItemId = item.Id,
                    Text = item.Question,
                    Answer = item.Answer,
                    Embedding = [1f, 0f]
                }
            ];
        }
    }
}