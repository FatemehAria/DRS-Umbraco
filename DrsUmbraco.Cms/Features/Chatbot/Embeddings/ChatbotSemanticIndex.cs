namespace DrsUmbraco.Cms.Features.Chatbot.Embeddings;

public sealed class ChatbotSemanticIndex
    : IChatbotSemanticIndex
{
    private ChatbotSemanticCandidate[] _candidates = [];

    public IReadOnlyList<ChatbotSemanticCandidate> GetAll()
    {
        return Volatile.Read(ref _candidates);
    }

    public void Replace(
        IReadOnlyList<ChatbotSemanticCandidate> candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);

        ChatbotSemanticCandidate[] snapshot = candidates.ToArray();

        Interlocked.Exchange(
            ref _candidates,
            snapshot);
    }
}