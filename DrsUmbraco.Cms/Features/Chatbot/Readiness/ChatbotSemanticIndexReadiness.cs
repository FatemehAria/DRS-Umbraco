namespace DrsUmbraco.Cms.Features.Chatbot.Readiness;

public sealed class ChatbotSemanticIndexReadiness
    : IChatbotSemanticIndexReadiness
{
    private int _isReady;

    public bool IsReady =>
        Volatile.Read(
            ref _isReady) == 1;

    public void MarkReady()
    {
        Interlocked.Exchange(
            ref _isReady,
            1);
    }

    public void MarkUnavailable()
    {
        Interlocked.Exchange(
            ref _isReady,
            0);
    }
}