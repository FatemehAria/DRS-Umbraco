using DrsUmbraco.Cms.Features.Chatbot.Models;

namespace DrsUmbraco.Cms.Features.Chatbot.Caching;

public interface IChatbotResponseCache
{
    long CaptureVersion();

    bool TryGet(
        string normalizedQuestion,
        long version,
        out ChatbotMessageResult? result);

    void Set(
        string normalizedQuestion,
        long version,
        ChatbotMessageResult result);

    void Invalidate();
}