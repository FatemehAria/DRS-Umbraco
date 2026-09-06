namespace DrsUmbraco.Cms.Features.Chatbot.Readiness;

public interface IChatbotSemanticIndexReadiness
{
    bool IsReady { get; }

    void MarkReady();

    void MarkUnavailable();
}