public interface IChatbotDiscriminativeEvidenceService
{
    ChatbotDiscriminativeEvidence Analyze(
        string question,
        int candidateLimit = 4);
}