using DrsUmbraco.Cms.Features.Chatbot.Models;

namespace DrsUmbraco.Cms.Features.Chatbot.Embeddings;

public sealed class ChatbotSemanticCandidateFactory
    : IChatbotSemanticCandidateFactory
{
    private readonly IEmbeddingService _embeddingService;

    public ChatbotSemanticCandidateFactory(
        IEmbeddingService embeddingService)
    {
        _embeddingService = embeddingService;
    }

    public IReadOnlyList<ChatbotSemanticCandidate> CreateCandidates(
        ChatbotKnowledgeItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        IEnumerable<string> texts =
            new[] { item.Question }
                .Concat(item.AlternativeQuestions)
                .Where(text =>
                    !string.IsNullOrWhiteSpace(text))
                .Select(text => text.Trim())
                .Distinct(StringComparer.Ordinal);

        List<ChatbotSemanticCandidate> candidates = [];

        foreach (string text in texts)
        {
            float[] embedding = _embeddingService.Generate(text);

            ChatbotSemanticCandidate candidate =
                new()
                {
                    KnowledgeItemId = item.Id,
                    Text = text,
                    Answer = item.Answer,
                    Embedding = embedding
                };

            candidates.Add(candidate);
        }

        return candidates;
    }
}