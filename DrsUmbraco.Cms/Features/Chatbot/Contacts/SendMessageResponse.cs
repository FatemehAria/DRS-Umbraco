using System.ComponentModel.DataAnnotations;
using DrsUmbraco.Cms.Features.Chatbot.Models;

namespace DrsUmbraco.Cms.Features.Chatbot.Contracts;

// برای پاسخ api
public sealed class SendMessageResponse
{
    public required ChatbotResponseType ResponseType
    {
        get;
        init;
    }

    [Required]
    public required string Reply
    {
        get;
        init;
    }

    public IReadOnlyList<ChatbotSuggestion> Suggestions
    {
        get;
        init;
    } = [];
}