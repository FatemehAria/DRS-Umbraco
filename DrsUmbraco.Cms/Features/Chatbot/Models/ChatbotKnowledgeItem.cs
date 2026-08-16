namespace DrsUmbraco.Cms.Features.Chatbot.Models;

public sealed class ChatbotKnowledgeItem
{
    public required Guid Id { get; init; }

    public required string Question { get; init; }

    public required string Answer { get; init; }

    public IReadOnlyList<string> AlternativeQuestions { get; init; }
    = [];

    public ChatbotKnowledgeItemKind Kind
    {
        get;
        init;
    } = ChatbotKnowledgeItemKind.Answer;
}