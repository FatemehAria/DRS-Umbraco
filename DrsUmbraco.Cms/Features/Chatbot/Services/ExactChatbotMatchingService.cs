using DrsUmbraco.Cms.Features.Chatbot.Models;
using DrsUmbraco.Cms.Features.Chatbot.Text;

namespace DrsUmbraco.Cms.Features.Chatbot.Services;

public sealed class ExactChatbotMatchingService
    : IChatbotMatchingService
{
    private readonly IChatbotKnowledgeService _knowledgeService;
    private readonly IPersianTextNormalizer _textNormalizer;

    public ExactChatbotMatchingService(
        IChatbotKnowledgeService knowledgeService,
        IPersianTextNormalizer textNormalizer)
    {
        _knowledgeService = knowledgeService;
        _textNormalizer = textNormalizer;
    }

    public ChatbotMatchResult FindMatch(string? question)
    {
        // 1. نرمال‌سازی سؤال کاربر
        string normalizedQuestion = _textNormalizer.Normalize(question);

        // 2. اگر خالی بود، نتیجه ناموفق برگردان
        if (string.IsNullOrEmpty(normalizedQuestion))
        {
            return new ChatbotMatchResult
            {
                IsMatch = false,
            };
        }

        // 3. دریافت FAQها
        var faqs = _knowledgeService.GetAll();

        foreach (ChatbotKnowledgeItem item in faqs)
        {
            if (item.Kind != ChatbotKnowledgeItemKind.Answer)
            {
                continue;
            }
            
            IEnumerable<string> candidateQuestions =
                new[] { item.Question }
                    .Concat(item.AlternativeQuestions);

            foreach (string candidateQuestion in candidateQuestions)
            {
                string normalizedCandidate =
                    _textNormalizer.Normalize(candidateQuestion);

                // 5. مقایسه با سؤال کاربر
                bool isMatch = string.Equals(
                    normalizedCandidate,
                    normalizedQuestion,
                    StringComparison.Ordinal);

                // در صورت Match، return

                if (isMatch)
                {
                    return new ChatbotMatchResult
                    {
                        IsMatch = true,
                        Answer = item.Answer,
                        KnowledgeItemId = item.Id
                    };
                }
            }
        }

        // 7. در غیر این صورت، نتیجه ناموفق برگردان
        return new ChatbotMatchResult
        {
            IsMatch = false,
        };
    }
}