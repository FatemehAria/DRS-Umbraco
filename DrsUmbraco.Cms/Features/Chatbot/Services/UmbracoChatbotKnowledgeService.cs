using DrsUmbraco.Cms.Features.Chatbot.Models;
using Umbraco.Cms.Core;
using Umbraco.Extensions;

namespace DrsUmbraco.Cms.Features.Chatbot.Services;

public sealed class UmbracoChatbotKnowledgeService
    : IChatbotKnowledgeService
{
    private readonly IPublishedContentQuery _publishedContentQuery;

    public UmbracoChatbotKnowledgeService(
        IPublishedContentQuery publishedContentQuery)
    {
        _publishedContentQuery = publishedContentQuery;
    }

    public IReadOnlyList<ChatbotKnowledgeItem> GetAll()
    {
        // 1. دریافت Rootهای منتشرشده
        var roots = _publishedContentQuery.ContentAtRoot();

        // 2. پیدا کردن chatbotKnowledgeBase
        var knowledgeBase = roots
            .SelectMany(root => root.Children())
            .FirstOrDefault(item =>
                item.ContentType.Alias == "chatbotKnowledgeBase");

        // 3. اگر پیدا نشد، collection خالی برگردان
        if (knowledgeBase is null)
        {
            return [];
        }

        // 4. خواندن فرزندان chatbotFaqItem
        var faqNodes = knowledgeBase
            .Children()
            .Where(item =>
                item.ContentType.Alias == "chatbotFaqItem");

        List<ChatbotKnowledgeItem> result = [];

        // 5. خواندن question و answer
        foreach (var item in faqNodes)
        {
            string? question = item.Value<string>("question")?.Trim();

            string? answer = item.Value<string>("answer")?.Trim();

            // 6. حذف آیتم‌های ناقص
            if (string.IsNullOrWhiteSpace(question) ||
                string.IsNullOrWhiteSpace(answer))
            {
                continue;
            }

            string[] alternativeQuestions =
                item.Value<string[]>("alternativeQuestions")
                ?? [];

            var cleanedAlternativeQuestions = alternativeQuestions
                .Where(question =>
                    !string.IsNullOrWhiteSpace(question))
                .Select(question => question.Trim())
                .Distinct(StringComparer.Ordinal)
                .ToArray();

            // 7. تبدیل به ChatbotKnowledgeItem
            var knowledgeItem = new ChatbotKnowledgeItem
            {
                Id = item.Key,
                Question = question,
                Answer = answer,
                AlternativeQuestions = cleanedAlternativeQuestions
            };

            result.Add(knowledgeItem);
        }

        return result;
    }
}