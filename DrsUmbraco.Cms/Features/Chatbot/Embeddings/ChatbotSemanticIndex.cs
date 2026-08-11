namespace DrsUmbraco.Cms.Features.Chatbot.Embeddings;

public sealed class ChatbotSemanticIndex
    : IChatbotSemanticIndex
{
    private ChatbotSemanticCandidate[] _candidates = [];

    private readonly object _sync = new();

    public IReadOnlyList<ChatbotSemanticCandidate> GetAll()
    {
        return Volatile.Read(ref _candidates);
    }

    public void Replace(
        IReadOnlyList<ChatbotSemanticCandidate> candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);

        ChatbotSemanticCandidate[] snapshot =
            candidates.ToArray();

        lock (_sync)
        {
            Volatile.Write(
                ref _candidates,
                snapshot);
        }
    }

    public void ReplaceForKnowledgeItem(
        Guid knowledgeItemId,
        IReadOnlyList<ChatbotSemanticCandidate> candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);

        lock (_sync)
        {
            ChatbotSemanticCandidate[] current =
                Volatile.Read(ref _candidates);

            ChatbotSemanticCandidate[] updated =
                current
                    .Where(candidate =>
                        candidate.KnowledgeItemId != knowledgeItemId)
                    .Concat(candidates)
                    .ToArray();

            Volatile.Write(
                ref _candidates,
                updated);
        }
    }

    public void RemoveForKnowledgeItem(
        Guid knowledgeItemId)
    {
        lock (_sync)
        {
            ChatbotSemanticCandidate[] current =
                Volatile.Read(ref _candidates);

            ChatbotSemanticCandidate[] updated =
                current
                    .Where(candidate =>
                        candidate.KnowledgeItemId != knowledgeItemId)
                    .ToArray();

            Volatile.Write(
                ref _candidates,
                updated);
        }
    }
}