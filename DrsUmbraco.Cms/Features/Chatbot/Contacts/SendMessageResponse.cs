namespace DrsUmbraco.Cms.Features.Chatbot.Contracts;

// برای پاسخ api
public sealed class SendMessageResponse
{
    public required string Reply { get; init; }
}