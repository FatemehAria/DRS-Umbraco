using DrsUmbraco.Cms.Features.Chatbot.Models;
using DrsUmbraco.Cms.Features.Chatbot.Services;
using DrsUmbraco.Cms.Features.Chatbot.Text;

namespace DrsUmbraco.Cms.Tests.Features.Chatbot.Services;

private sealed class FakeChatbotKnowledgeService
    : IChatbotKnowledgeService
{
    private readonly IReadOnlyList<ChatbotKnowledgeItem> _items;

    public FakeChatbotKnowledgeService(
        IReadOnlyList<ChatbotKnowledgeItem> items)
    {
        _items = items;
    }

    public IReadOnlyList<ChatbotKnowledgeItem> GetAll()
    {
        return _items;
    }

    [Fact]
    public void FindMatch_WhenPrimaryQuestionMatches_ShouldReturnAnswer()
    {
        // Arrange
        var knowledgeItems = new List<ChatbotKnowledgeItem>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Question = "چطور رمز عبورم را تغییر بدهم؟",
                Answer = "پاسخ تغییر رمز",
                AlternativeQuestions = []
            }
        };

        IChatbotKnowledgeService knowledgeService = new FakeChatbotKnowledgeService(knowledgeItems);

        PersianTextNormalizer normalizer = new();

        ExactChatbotMatchingService matchingService = new(knowledgeService, normalizer);

        // Act
        ChatbotMatchResult result = matchingService.FindMatch("  چطور رمز عبورم را تغيير بدهم؟ ");

        // Assert
        Assert.True(result.IsMatch, true);
        Assert.Equal(result.answer, knowledgeItems[0].Answer);
        Assert.Equal(result.KnowledgeItemId, knowledgeItems[0].Id);
    }
}