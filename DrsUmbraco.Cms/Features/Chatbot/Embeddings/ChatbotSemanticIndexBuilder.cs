using DrsUmbraco.Cms.Features.Chatbot.Models;
using DrsUmbraco.Cms.Features.Chatbot.Services;

namespace DrsUmbraco.Cms.Features.Chatbot.Embeddings;

public sealed class ChatbotSemanticIndexBuilder
    : IChatbotSemanticIndexBuilder
{
    private readonly IChatbotKnowledgeService _knowledgeService;
    private readonly IChatbotSemanticCandidateFactory _candidateFactory;
    private readonly IChatbotSemanticIndex _semanticIndex;

    public ChatbotSemanticIndexBuilder(
        IChatbotKnowledgeService knowledgeService,
        IChatbotSemanticCandidateFactory candidateFactory,
        IChatbotSemanticIndex semanticIndex)
    {
        _knowledgeService = knowledgeService;
        _candidateFactory = candidateFactory;
        _semanticIndex = semanticIndex;
    }

    public int Rebuild()
    {
        IReadOnlyList<ChatbotKnowledgeItem> knowledgeItems = _knowledgeService.GetAll();

        List<ChatbotSemanticCandidate> candidates = [];

        foreach (ChatbotKnowledgeItem item in knowledgeItems)
        {
            IReadOnlyList<ChatbotSemanticCandidate>
                itemCandidates =
                    _candidateFactory.CreateCandidates(item);

            candidates.AddRange(itemCandidates);
        }

        _semanticIndex.Replace(candidates);

        return candidates.Count;
    }
}