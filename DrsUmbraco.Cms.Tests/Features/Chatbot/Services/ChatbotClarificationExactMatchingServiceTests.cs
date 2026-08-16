using DrsUmbraco.Cms.Features.Chatbot.Models;
using DrsUmbraco.Cms.Features.Chatbot.Services;
using DrsUmbraco.Cms.Features.Chatbot.Text;
using Xunit;

public sealed class
    ChatbotClarificationExactMatchingServiceTests
{
    [Fact]
    public void Find_WhenAlternativeQuestionMatchesClarification_ShouldReturnMatch()
    {
        // Arrange

        Guid clarificationId =
            Guid.NewGuid();

        ChatbotKnowledgeItem clarification =
            new()
            {
                Id =
                    clarificationId,

                Question =
                    "می‌خواهم یکی از اطلاعات حسابم را تغییر بدهم",

                Answer =
                    "لطفاً مشخص کنید کدام اطلاعات را می‌خواهید تغییر دهید.",

                AlternativeQuestions =
                [
                    "میخوام یکی از اطلاعات حسابم رو عوض کنم"
                ],

                Kind =
                    ChatbotKnowledgeItemKind.Clarification
            };

        FakeKnowledgeService knowledgeService =
            new(
                [
                    clarification
                ]);

        PersianTextNormalizer normalizer =
            new();

        ChatbotClarificationExactMatchingService service =
            new(
                knowledgeService,
                normalizer);

        // Act

        ChatbotMatchResult result =
            service.Find(
                "میخوام یکی از اطلاعات حسابم رو عوض کنم");

        // Assert

        Assert.True(
            result.IsMatch);

        Assert.Equal(
            clarificationId,
            result.KnowledgeItemId);

        Assert.Equal(
            clarification.Answer,
            result.Answer);
    }


    [Fact]
    public void Find_WhenNormalAnswerMatches_ShouldIgnoreIt()
    {
        // Arrange

        ChatbotKnowledgeItem normalAnswer =
            new()
            {
                Id =
                    Guid.NewGuid(),

                Question =
                    "چطور شماره موبایل حسابم را تغییر بدهم؟",

                Answer =
                    "برای تغییر شماره موبایل وارد تنظیمات شوید.",

                AlternativeQuestions =
                [
                    "شماره موبایلم رو میخوام تغییر بدم"
                ],

                Kind =
                    ChatbotKnowledgeItemKind.Answer
            };

        FakeKnowledgeService knowledgeService =
            new(
                [
                    normalAnswer
                ]);

        PersianTextNormalizer normalizer =
            new();

        ChatbotClarificationExactMatchingService service =
            new(
                knowledgeService,
                normalizer);

        // Act

        ChatbotMatchResult result =
            service.Find(
                "شماره موبایلم رو میخوام تغییر بدم");

        // Assert

        Assert.False(
            result.IsMatch);

        Assert.Null(
            result.KnowledgeItemId);

        Assert.Null(
            result.Answer);
    }


    [Fact]
    public void Find_WhenQuestionIsBlank_ShouldReturnNoMatch()
    {
        // Arrange

        FakeKnowledgeService knowledgeService =
            new([]);

        PersianTextNormalizer normalizer =
            new();

        ChatbotClarificationExactMatchingService service =
            new(
                knowledgeService,
                normalizer);

        // Act

        ChatbotMatchResult result =
            service.Find("   ");

        // Assert

        Assert.False(
            result.IsMatch);

        Assert.Null(
            result.KnowledgeItemId);

        Assert.Null(
            result.Answer);
    }


    private sealed class FakeKnowledgeService
        : IChatbotKnowledgeService
    {
        private readonly IReadOnlyList<ChatbotKnowledgeItem>
            _items;

        public FakeKnowledgeService(
            IReadOnlyList<ChatbotKnowledgeItem> items)
        {
            _items = items;
        }

        public IReadOnlyList<ChatbotKnowledgeItem> GetAll()
        {
            return _items;
        }

        public ChatbotKnowledgeItem? GetById(
            Guid id)
        {
            return _items.FirstOrDefault(
                item =>
                    item.Id == id);
        }
    }
}