using DrsUmbraco.Cms.Features.Chatbot.Models;
using DrsUmbraco.Cms.Features.Chatbot.Services;
using DrsUmbraco.Cms.Features.Chatbot.Text;

namespace DrsUmbraco.Cms.Tests.Features.Chatbot.Services;

public sealed class ExactChatbotMatchingServiceTests
{
    [Fact]
    public void FindMatch_WhenPrimaryQuestionMatches_ShouldReturnAnswer()
    {
        // Arrange
        Guid knowledgeItemId = Guid.NewGuid();

        var knowledgeItems = new List<ChatbotKnowledgeItem>
        {
            new()
            {
                Id = knowledgeItemId,
                Question = "چطور رمز عبورم را تغییر بدهم؟",
                Answer = "پاسخ تغییر رمز",
                AlternativeQuestions = []
            }
        };

        IChatbotKnowledgeService knowledgeService =
            new FakeChatbotKnowledgeService(knowledgeItems);

        PersianTextNormalizer normalizer = new();

        ExactChatbotMatchingService matchingService =
            new(knowledgeService, normalizer);

        // Act
        ChatbotMatchResult result =
            matchingService.FindMatch(
                "  چطور رمز عبورم را تغيير بدهم؟ ");

        // Assert
        Assert.True(result.IsMatch);
        Assert.Equal("پاسخ تغییر رمز", result.Answer);
        Assert.Equal(knowledgeItemId, result.KnowledgeItemId);
    }

    [Fact]
    public void FindMatch_WhenAlternativeQuestionMatches_ShouldReturnAnswer()
    {
        // Arrange
        Guid knowledgeItemId = Guid.NewGuid();

        var knowledgeItems = new List<ChatbotKnowledgeItem>
        {
            new()
            {
                Id = knowledgeItemId,
                Question = "چطور رمز عبورم را تغییر بدهم؟",
                Answer = "پاسخ تغییر رمز",
                AlternativeQuestions =
                [
                    "چگونه پسوردم را عوض کنم؟"
                ]
            }
        };

        IChatbotKnowledgeService knowledgeService =
            new FakeChatbotKnowledgeService(knowledgeItems);

        PersianTextNormalizer normalizer = new();

        ExactChatbotMatchingService matchingService =
            new(knowledgeService, normalizer);

        // Act
        ChatbotMatchResult result =
            matchingService.FindMatch(
                "چگونه پسوردم را عوض كنم؟");

        // Assert
        Assert.True(result.IsMatch);
        Assert.Equal("پاسخ تغییر رمز", result.Answer);
        Assert.Equal(knowledgeItemId, result.KnowledgeItemId);
    }

    [Fact]
    public void FindMatch_WhenQuestionDoesNotMatch_ShouldReturnUnmatchedResult()
    {
        // Arrange
        var knowledgeItems = new List<ChatbotKnowledgeItem>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Question = "چطور رمز عبورم را تغییر بدهم؟",
                Answer = "پاسخ تغییر رمز",
                AlternativeQuestions =
                [
                    "چگونه پسوردم را عوض کنم؟"
                ]
            }
        };

        IChatbotKnowledgeService knowledgeService =
            new FakeChatbotKnowledgeService(knowledgeItems);

        PersianTextNormalizer normalizer = new();

        ExactChatbotMatchingService matchingService =
            new(knowledgeService, normalizer);

        // Act
        ChatbotMatchResult result =
            matchingService.FindMatch(
                "ساعت کاری شرکت چیست؟");

        // Assert
        Assert.False(result.IsMatch);
        Assert.Null(result.Answer);
        Assert.Null(result.KnowledgeItemId);
    }

    [Fact]
    public void FindMatch_WhenQuestionIsWhitespace_ShouldReturnUnmatchedResult()
    {
        // Arrange
        var knowledgeItems = new List<ChatbotKnowledgeItem>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Question = "چطور رمز عبورم را تغییر بدهم؟",
                Answer = "پاسخ تغییر رمز"
            }
        };

        IChatbotKnowledgeService knowledgeService =
            new FakeChatbotKnowledgeService(knowledgeItems);

        PersianTextNormalizer normalizer = new();

        ExactChatbotMatchingService matchingService =
            new(knowledgeService, normalizer);

        // Act
        ChatbotMatchResult result =
            matchingService.FindMatch("   ");

        // Assert
        Assert.False(result.IsMatch);
        Assert.Null(result.Answer);
        Assert.Null(result.KnowledgeItemId);
    }

    [Fact]
    public void FindMatch_WhenClarificationMatchesExactly_ShouldIgnoreIt()
    {
        // Arrange
        ChatbotKnowledgeItem clarification =
            new()
            {
                Id = Guid.NewGuid(),
                Question =
                    "میخوام یکی از اطلاعات حسابم رو عوض کنم",
                Answer =
                    "لطفاً مشخص کنید کدام اطلاعات را می‌خواهید تغییر دهید.",
                AlternativeQuestions = [],
                Kind =
                    ChatbotKnowledgeItemKind.Clarification
            };

        var knowledgeItems =
            new List<ChatbotKnowledgeItem>
            {
            clarification
            };

        IChatbotKnowledgeService knowledgeService =
            new FakeChatbotKnowledgeService(
                knowledgeItems);

        PersianTextNormalizer normalizer =
            new();

        ExactChatbotMatchingService matchingService =
            new(
                knowledgeService,
                normalizer);

        // Act
        ChatbotMatchResult result =
            matchingService.FindMatch(
                "میخوام یکی از اطلاعات حسابم رو عوض کنم");

        // Assert
        Assert.False(result.IsMatch);
        Assert.Null(result.KnowledgeItemId);
        Assert.Null(result.Answer);
    }
    
    private sealed class FakeChatbotKnowledgeService
        : IChatbotKnowledgeService
    {
        private readonly IReadOnlyList<ChatbotKnowledgeItem> _items;

        public FakeChatbotKnowledgeService(
            IReadOnlyList<ChatbotKnowledgeItem> items)
        {
            _items = items;
        }

        public ChatbotKnowledgeItem? GetById(Guid id)
        {
            return _items.FirstOrDefault(
                item => item.Id == id);
        }

        public IReadOnlyList<ChatbotKnowledgeItem> GetAll()
        {
            return _items;
        }
    }
}