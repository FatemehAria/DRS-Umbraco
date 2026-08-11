using DrsUmbraco.Cms.Features.Chatbot.Models;
using DrsUmbraco.Cms.Features.Chatbot.Services;

namespace DrsUmbraco.Cms.Features.Chatbot.Embeddings;

public sealed class ChatbotSemanticIndexUpdater
    : IChatbotSemanticIndexUpdater
{
    private readonly IChatbotKnowledgeService _knowledgeService;
    private readonly IChatbotSemanticCandidateFactory _candidateFactory;
    private readonly IChatbotSemanticIndex _semanticIndex;

    public ChatbotSemanticIndexUpdater(
        IChatbotKnowledgeService knowledgeService,
        IChatbotSemanticCandidateFactory candidateFactory,
        IChatbotSemanticIndex semanticIndex)
    {
        _knowledgeService = knowledgeService;
        _candidateFactory = candidateFactory;
        _semanticIndex = semanticIndex;
    }

    public bool Refresh(Guid knowledgeItemId)
    {
        ChatbotKnowledgeItem? item =
            _knowledgeService
                .GetAll()
                .FirstOrDefault(item =>
                    item.Id == knowledgeItemId);

        if (item is null)
        {
            _semanticIndex.RemoveForKnowledgeItem(
                knowledgeItemId);

            return false;
        }

        IReadOnlyList<ChatbotSemanticCandidate> candidates =
            _candidateFactory.CreateCandidates(item);

        _semanticIndex.ReplaceForKnowledgeItem(
            knowledgeItemId,
            candidates);

        return true;
    }
}