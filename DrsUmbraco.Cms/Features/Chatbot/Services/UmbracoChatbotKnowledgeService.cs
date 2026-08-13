using DrsUmbraco.Cms.Features.Chatbot.Models;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.PublishedContent;
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

        foreach (var faqNode in faqNodes)
        {
            ChatbotKnowledgeItem? knowledgeItem =
                Map(faqNode);

            if (knowledgeItem is not null)
            {
                result.Add(knowledgeItem);
            }
        }

        return result;
    }

    public ChatbotKnowledgeItem? GetById(Guid id)
    {
        IPublishedContent? content =
            _publishedContentQuery.Content(id);

        if (content is null)
        {
            return null;
        }

        if (content.ContentType.Alias != "chatbotFaqItem")
        {
            return null;
        }

        return Map(content);
    }

    private static ChatbotKnowledgeItem? Map(
       IPublishedContent item)
    {
        string? question =
            item.Value<string>("question")?.Trim();

        string? answer =
            item.Value<string>("answer")?.Trim();

        if (string.IsNullOrWhiteSpace(question) ||
            string.IsNullOrWhiteSpace(answer))
        {
            return null;
        }

        string[] alternativeQuestions =
            item.Value<string[]>("alternativeQuestions")
            ?? [];

        string[] cleanedAlternativeQuestions =
            alternativeQuestions
                .Where(question =>
                    !string.IsNullOrWhiteSpace(question))
                .Select(question => question.Trim())
                .Distinct(StringComparer.Ordinal)
                .ToArray();

        return new ChatbotKnowledgeItem
        {
            Id = item.Key,
            Question = question,
            Answer = answer,
            AlternativeQuestions =
                cleanedAlternativeQuestions
        };
    }

}