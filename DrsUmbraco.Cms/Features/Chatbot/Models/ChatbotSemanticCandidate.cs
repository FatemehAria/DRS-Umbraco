namespace DrsUmbraco.Cms.Features.Chatbot.Embeddings;

public sealed class ChatbotSemanticCandidate
{
    public required Guid KnowledgeItemId { get; init; }

    public required string Text { get; init; }

    public required string Answer { get; init; }

    public required float[] Embedding { get; init; }
}