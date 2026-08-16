using DrsUmbraco.Cms.Features.Chatbot.Models;

namespace DrsUmbraco.Cms.Features.Chatbot.Services;

public interface IChatbotCandidateIntentSetEvidenceService
{
    ChatbotCandidateIntentSetEvidence? Analyze(
        string question);
}