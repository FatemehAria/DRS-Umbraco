using DrsUmbraco.Cms.Features.Chatbot.Embeddings;
using DrsUmbraco.Cms.Features.Chatbot.Models;
using DrsUmbraco.Cms.Features.Chatbot.Services;

public sealed class ChatbotSemanticRankingService
    : IChatbotSemanticRankingService
{
    private readonly IEmbeddingService _embeddingService;
    private readonly IChatbotSemanticIndex _semanticIndex;

    public ChatbotSemanticRankingService(
        IEmbeddingService embeddingService,
        IChatbotSemanticIndex semanticIndex)
    {
        _embeddingService = embeddingService;
        _semanticIndex = semanticIndex;
    }

    public IReadOnlyList<ChatbotSemanticRankedResult> FindTop(
        string question,
        int limit,
        ChatbotKnowledgeItemKind? kind = null)
    {
        if (string.IsNullOrWhiteSpace(question))
        {
            return [];
        }

        if (limit <= 0)
        {
            return [];
        }

        IReadOnlyList<ChatbotSemanticCandidate> candidates =
            _semanticIndex.GetAll();

        if (candidates.Count == 0)
        {
            return [];
        }

        if (kind.HasValue)
        {
            candidates =
                candidates
                    .Where(candidate =>
                        candidate.Kind == kind.Value)
                    .ToArray();
        }

        float[] questionEmbedding =
            _embeddingService.Generate(question);

        Dictionary<Guid, ScoredCandidate>
            bestByKnowledgeItem = [];

        foreach (ChatbotSemanticCandidate candidate in candidates)
        {
            float score =
                CosineSimilarityCalculator.Calculate(
                    questionEmbedding,
                    candidate.Embedding);

            ScoredCandidate scoredCandidate =
                new()
                {
                    Candidate = candidate,
                    Score = score
                };

            if (!bestByKnowledgeItem.TryGetValue(
                    candidate.KnowledgeItemId,
                    out ScoredCandidate? existing) ||
                score > existing.Score)
            {
                bestByKnowledgeItem[
                    candidate.KnowledgeItemId] =
                    scoredCandidate;
            }
        }

        return bestByKnowledgeItem
            .Values
            .OrderByDescending(item => item.Score)
            .Take(limit)
            .Select(
                (item, index) =>
                    new ChatbotSemanticRankedResult
                    {
                        Rank = index + 1,

                        KnowledgeItemId =
                            item.Candidate.KnowledgeItemId,

                        MatchedText =
                            item.Candidate.Text,

                        Answer =
                            item.Candidate.Answer,

                        Score =
                            item.Score
                    })
            .ToArray();
    }

    private sealed class ScoredCandidate
    {
        public required ChatbotSemanticCandidate Candidate
        {
            get;
            init;
        }

        public required float Score
        {
            get;
            init;
        }
    }
}