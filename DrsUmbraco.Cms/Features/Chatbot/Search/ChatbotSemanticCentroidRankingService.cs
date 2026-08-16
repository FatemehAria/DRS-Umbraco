using DrsUmbraco.Cms.Features.Chatbot.Embeddings;
using DrsUmbraco.Cms.Features.Chatbot.Models;

public sealed class ChatbotSemanticCentroidRankingService
    : IChatbotSemanticCentroidRankingService
{
    private readonly IEmbeddingService _embeddingService;
    private readonly IChatbotSemanticIndex _semanticIndex;
    private readonly IEmbeddingCentroidCalculator _centroidCalculator;

    public ChatbotSemanticCentroidRankingService(
        IEmbeddingService embeddingService,
        IChatbotSemanticIndex semanticIndex,
        IEmbeddingCentroidCalculator centroidCalculator)
    {
        _embeddingService = embeddingService;
        _semanticIndex = semanticIndex;
        _centroidCalculator = centroidCalculator;
    }

    public IReadOnlyList<ChatbotSemanticRankedResult> FindTop(
        string question,
        int limit,
        ChatbotKnowledgeItemKind? kind = null)
    {
        if (string.IsNullOrWhiteSpace(question) ||
            limit <= 0)
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

        var groupedCandidates =
            candidates.GroupBy(
                candidate => candidate.KnowledgeItemId);

        List<ScoredKnowledgeItem> scoredItems = [];

        foreach (var group in groupedCandidates)
        {
            ChatbotSemanticCandidate[] faqCandidates =
                group.ToArray();

            float[][] embeddings =
                faqCandidates
                    .Select(candidate => candidate.Embedding)
                    .ToArray();

            float[] centroid =
                _centroidCalculator.Calculate(embeddings);

            float score =
                CosineSimilarityCalculator.Calculate(
                    questionEmbedding,
                    centroid);

            ChatbotSemanticCandidate representative =
                faqCandidates[0];

            scoredItems.Add(
                new ScoredKnowledgeItem
                {
                    KnowledgeItemId = group.Key,
                    Answer = representative.Answer,
                    Score = score
                });
        }

        return scoredItems
            .OrderByDescending(item => item.Score)
            .Take(limit)
            .Select(
                (item, index) =>
                    new ChatbotSemanticRankedResult
                    {
                        Rank = index + 1,
                        KnowledgeItemId =
                            item.KnowledgeItemId,

                        MatchedText =
                            "[FAQ Centroid]",

                        Answer =
                            item.Answer,

                        Score =
                            item.Score
                    })
            .ToArray();
    }

    private sealed class ScoredKnowledgeItem
    {
        public required Guid KnowledgeItemId
        {
            get;
            init;
        }

        public required string Answer
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