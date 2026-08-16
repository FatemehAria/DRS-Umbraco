namespace DrsUmbraco.Cms.Features.Chatbot.Models;

public sealed class ChatbotSuggestion
{
    public required Guid KnowledgeItemId { get; init; }

    public required string Label { get; init; }
}