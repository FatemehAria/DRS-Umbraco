namespace DrsUmbraco.Cms.Features.Chatbot.Models;

public sealed class ChatbotMatchResult
{
    public required bool IsMatch { get; init; }

    public Guid? KnowledgeItemId { get; init; }

    public string? Answer { get; init; }
}