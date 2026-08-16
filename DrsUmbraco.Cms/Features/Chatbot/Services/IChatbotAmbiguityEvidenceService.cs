
using DrsUmbraco.Cms.Features.Chatbot.Services;

public interface IChatbotAmbiguityEvidenceService
{
    ChatbotAmbiguityEvidence? Analyze(
        string question);
}