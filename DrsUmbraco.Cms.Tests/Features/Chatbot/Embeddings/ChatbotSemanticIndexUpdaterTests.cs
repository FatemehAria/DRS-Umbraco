using DrsUmbraco.Cms.Features.Chatbot.Embeddings;
using DrsUmbraco.Cms.Features.Chatbot.Models;
using DrsUmbraco.Cms.Features.Chatbot.Services;

namespace DrsUmbraco.Cms.Tests.Features.Chatbot.Embeddings;

public sealed class ChatbotSemanticIndexUpdaterTests
{
    [Fact]
    public void Refresh_WhenKnowledgeItemExists_ShouldReplaceOnlyThatItem()
    {
        Guid passwordId = Guid.NewGuid();
        Guid supportId = Guid.NewGuid();

        ChatbotKnowledgeItem passwordItem =
            new()
            {
                Id = passwordId,
                Question = "New password question",
                Answer = "Password answer"
            };

        ChatbotKnowledgeItem supportItem =
            new()
            {
                Id = supportId,
                Question = "Support question",
                Answer = "Support answer"
            };

        FakeKnowledgeService knowledgeService =
            new([
                passwordItem,
                supportItem
            ]);

        FakeCandidateFactory candidateFactory = new();

        ChatbotSemanticIndex semanticIndex = new();

        ChatbotSemanticCandidate oldPasswordCandidate =
            CreateCandidate(
                passwordId,
                "Old password question");

        ChatbotSemanticCandidate supportCandidate =
            CreateCandidate(
                supportId,
                "Support question");

        semanticIndex.Replace([
            oldPasswordCandidate,
            supportCandidate
        ]);

        ChatbotSemanticIndexUpdater updater =
            new(
                knowledgeService,
                candidateFactory,
                semanticIndex);

        bool result =
            updater.Refresh(passwordId);

        Assert.True(result);

        IReadOnlyList<ChatbotSemanticCandidate> candidates =
            semanticIndex.GetAll();

        Assert.Equal(2, candidates.Count);

        Assert.DoesNotContain(
            candidates,
            candidate =>
                candidate.Text == "Old password question");

        Assert.Contains(
            candidates,
            candidate =>
                candidate.Text == "New password question");

        Assert.Contains(
            supportCandidate,
            candidates);
    }

    [Fact]
    public void Refresh_ShouldCreateCandidatesOnlyForRequestedKnowledgeItem()
    {
        Guid passwordId = Guid.NewGuid();
        Guid supportId = Guid.NewGuid();

        ChatbotKnowledgeItem passwordItem =
            CreateKnowledgeItem(
                passwordId,
                "Password");

        ChatbotKnowledgeItem supportItem =
            CreateKnowledgeItem(
                supportId,
                "Support");

        FakeKnowledgeService knowledgeService =
            new([
                passwordItem,
                supportItem
            ]);

        FakeCandidateFactory candidateFactory = new();

        ChatbotSemanticIndex semanticIndex = new();

        ChatbotSemanticIndexUpdater updater =
            new(
                knowledgeService,
                candidateFactory,
                semanticIndex);

        updater.Refresh(passwordId);

        Assert.Single(
            candidateFactory.CreatedForItems);

        Assert.Equal(
            passwordId,
            candidateFactory.CreatedForItems[0]);
    }

    [Fact]
    public void Refresh_WhenKnowledgeItemDoesNotExist_ShouldRemoveItFromIndex()
    {
        Guid deletedFaqId = Guid.NewGuid();
        Guid remainingFaqId = Guid.NewGuid();

        FakeKnowledgeService knowledgeService =
            new([]);

        FakeCandidateFactory candidateFactory = new();

        ChatbotSemanticIndex semanticIndex = new();

        ChatbotSemanticCandidate deletedCandidate =
            CreateCandidate(
                deletedFaqId,
                "Deleted FAQ");

        ChatbotSemanticCandidate remainingCandidate =
            CreateCandidate(
                remainingFaqId,
                "Remaining FAQ");

        semanticIndex.Replace([
            deletedCandidate,
            remainingCandidate
        ]);

        ChatbotSemanticIndexUpdater updater =
            new(
                knowledgeService,
                candidateFactory,
                semanticIndex);

        bool result =
            updater.Refresh(deletedFaqId);

        Assert.False(result);

        IReadOnlyList<ChatbotSemanticCandidate> candidates =
            semanticIndex.GetAll();

        Assert.Single(candidates);

        Assert.Contains(
            remainingCandidate,
            candidates);

        Assert.DoesNotContain(
            deletedCandidate,
            candidates);

        Assert.Empty(
            candidateFactory.CreatedForItems);
    }

    private static ChatbotKnowledgeItem CreateKnowledgeItem(
        Guid id,
        string question)
    {
        return new ChatbotKnowledgeItem
        {
            Id = id,
            Question = question,
            Answer = $"Answer for {question}"
        };
    }

    private static ChatbotSemanticCandidate CreateCandidate(
        Guid knowledgeItemId,
        string text)
    {
        return new ChatbotSemanticCandidate
        {
            KnowledgeItemId = knowledgeItemId,
            Text = text,
            Answer = $"Answer for {text}",
            Embedding = [1f, 0f],
            Kind = ChatbotKnowledgeItemKind.Clarification
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

        public ChatbotKnowledgeItem? GetById(Guid id)
        {
            return _items.FirstOrDefault(
                item => item.Id == id);
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
                    Embedding = [1f, 0f],
                    Kind = ChatbotKnowledgeItemKind.Clarification
                }
            ];
        }
    }
}