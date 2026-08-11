using DrsUmbraco.Cms.Features.Chatbot.Models;

namespace DrsUmbraco.Cms.Features.Chatbot.Embeddings;

public interface IChatbotSemanticCandidateFactory
{
    IReadOnlyList<ChatbotSemanticCandidate> CreateCandidates(
        ChatbotKnowledgeItem item);
}