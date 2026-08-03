namespace DrsUmbraco.Cms.Models;

public sealed class ChatbotKnowledgeItem
{
    public required Guid Id { get; init; }

    public required string Question { get; init; }

    public required string Answer { get; init; }
}