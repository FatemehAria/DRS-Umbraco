namespace DrsUmbraco.Cms.Features.Chatbot.Models;

public sealed class ChatbotMessageResult
{
    public required ChatbotResponseType ResponseType { get; init; }

    public required string Reply { get; init; }

    public IReadOnlyList<ChatbotSuggestion> Suggestions
    {
        get;
        init;
    } = [];
}