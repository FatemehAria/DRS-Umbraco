namespace DrsUmbraco.Cms.Features.Chatbot.Contracts;

// برای دریافت پیام کاربر
public sealed class SendMessageRequest
{
    public string? Message { get; init; }
}