using DrsUmbraco.Cms.Features.Chatbot.Caching;
using DrsUmbraco.Cms.Features.Chatbot.Models;
using DrsUmbraco.Cms.Features.Chatbot.Services;

namespace DrsUmbraco.Cms.Features.Chatbot.Embeddings;

public sealed class ChatbotSemanticIndexUpdater
    : IChatbotSemanticIndexUpdater
{
    private readonly IChatbotKnowledgeService _knowledgeService;
    private readonly IChatbotSemanticCandidateFactory _candidateFactory;
    private readonly IChatbotSemanticIndex _semanticIndex;
    private readonly IChatbotResponseCache _responseCache;
    public ChatbotSemanticIndexUpdater(
        IChatbotKnowledgeService knowledgeService,
        IChatbotSemanticCandidateFactory candidateFactory,
        IChatbotSemanticIndex semanticIndex,
        IChatbotResponseCache responseCache)
    {
        _knowledgeService = knowledgeService;
        _candidateFactory = candidateFactory;
        _semanticIndex = semanticIndex;
        _responseCache = responseCache;
    }

    public bool Refresh(Guid knowledgeItemId)
    {
        ChatbotKnowledgeItem? item = _knowledgeService.GetById(knowledgeItemId);

        if (item is null)
        {
            _semanticIndex.RemoveForKnowledgeItem(knowledgeItemId);

            _responseCache.Invalidate();

            return false;
        }

        IReadOnlyList<ChatbotSemanticCandidate> candidates = _candidateFactory.CreateCandidates(item);

        _semanticIndex.ReplaceForKnowledgeItem(
            knowledgeItemId,
            candidates);

        _responseCache.Invalidate();

        return true;
    }
}