using DrsUmbraco.Cms.Features.Chatbot.Caching;
using DrsUmbraco.Cms.Features.Chatbot.Embeddings;
using DrsUmbraco.Cms.Features.Chatbot.Models;
using DrsUmbraco.Cms.Features.Chatbot.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace DrsUmbraco.Cms.Tests.Features.Chatbot.Embeddings;

public sealed class ChatbotSemanticIndexBuilderTests
{
    [Fact]
    public void Rebuild_ShouldBuildCandidatesForAllKnowledgeItems()
    {
        ChatbotKnowledgeItem firstItem = CreateKnowledgeItem("Question 1");

        ChatbotKnowledgeItem secondItem = CreateKnowledgeItem("Question 2");

        FakeKnowledgeService knowledgeService = new([firstItem, secondItem]);

        FakeCandidateFactory candidateFactory = new();

        FakeChatbotResponseCache responseCache = new();

        ChatbotSemanticIndex semanticIndex = new();

        EmbeddingPerformanceMetrics performanceMetrics = new();

        ChatbotSemanticIndexBuilder builder =
            new(
                knowledgeService,
                candidateFactory,
                semanticIndex,
                NullLogger<ChatbotSemanticIndexBuilder>.Instance,
                performanceMetrics,
                responseCache);

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
                Embedding = [1f, 0f],
                Kind = ChatbotKnowledgeItemKind.Clarification
            }
        ]);

        ChatbotKnowledgeItem newItem =
            CreateKnowledgeItem("New question");

        FakeKnowledgeService knowledgeService =
            new([newItem]);

        FakeCandidateFactory candidateFactory = new();

        FakeChatbotResponseCache responseCache = new();

        EmbeddingPerformanceMetrics performanceMetrics = new();

        ChatbotSemanticIndexBuilder builder =
            new(
                knowledgeService,
                candidateFactory,
                semanticIndex,
                NullLogger<ChatbotSemanticIndexBuilder>.Instance,
                performanceMetrics,
                responseCache);

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

    [Fact]
    public void Rebuild_WhenIndexReplaceSucceeds_ShouldInvalidateResponseCache()
    {
        // Arrange
        ChatbotKnowledgeItem item = CreateKnowledgeItem("New question");

        FakeKnowledgeService knowledgeService = new([item]);

        FakeCandidateFactory candidateFactory = new();

        ChatbotSemanticIndex semanticIndex = new();

        EmbeddingPerformanceMetrics performanceMetrics = new();

        FakeChatbotResponseCache responseCache = new();

        ChatbotSemanticIndexBuilder builder =
            new(
                knowledgeService,
                candidateFactory,
                semanticIndex,
                NullLogger<ChatbotSemanticIndexBuilder>.Instance,
                performanceMetrics,
                responseCache);

        // Act
        builder.Rebuild();

        // Assert
        Assert.Equal(
            1,
            responseCache.InvalidateCallCount);
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

    private sealed class FakeChatbotResponseCache : IChatbotResponseCache
    {
        public int InvalidateCallCount
        {
            get;
            private set;
        }

        public long CaptureVersion()
        {
            return 0;
        }

        public bool TryGet(
            string normalizedQuestion,
            long version,
            out ChatbotMessageResult? result)
        {
            result = null;
            return false;
        }

        public void Set(
            string normalizedQuestion,
            long version,
            ChatbotMessageResult result)
        {
        }

        public void Invalidate()
        {
            InvalidateCallCount++;
        }
    }

    private sealed class FakeKnowledgeService : IChatbotKnowledgeService
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

        public ChatbotKnowledgeItem? GetById(Guid id)
        {
            return _items.FirstOrDefault(
                item => item.Id == id);
        }
    }

    private sealed class FakeCandidateFactory : IChatbotSemanticCandidateFactory
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
                    Embedding = [1f, 0f],
                    Kind = ChatbotKnowledgeItemKind.Clarification
                }
            ];
        }
    }
}