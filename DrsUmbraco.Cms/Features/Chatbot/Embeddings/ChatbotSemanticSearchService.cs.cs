namespace DrsUmbraco.Cms.Features.Chatbot.Embeddings;

public sealed class ChatbotSemanticSearchService
    : IChatbotSemanticSearchService
{
    private readonly IEmbeddingService _embeddingService;
    private readonly IChatbotSemanticIndex _semanticIndex;

    public ChatbotSemanticSearchService(
        IEmbeddingService embeddingService,
        IChatbotSemanticIndex semanticIndex)
    {
        _embeddingService = embeddingService;
        _semanticIndex = semanticIndex;
    }

    public ChatbotSemanticSearchResult? FindBest(
        string question,
        ChatbotKnowledgeItemKind? kind = null)
    {
        if (string.IsNullOrWhiteSpace(question))
        {
            throw new ArgumentException(
                "Question is required.",
                nameof(question));
        }

        IReadOnlyList<ChatbotSemanticCandidate> candidates =
            _semanticIndex.GetAll();

        if (candidates.Count == 0)
        {
            return null;
        }

        if (kind.HasValue)
        {
            candidates = candidates
                .Where(candidate =>
                    candidate.Kind == kind.Value)
                .ToArray();
        }
        
        float[] questionEmbedding =
            _embeddingService.Generate(question);

        Dictionary<Guid, ScoredCandidate>
            bestCandidateByKnowledgeItem = [];

        foreach (ChatbotSemanticCandidate candidate in candidates)
        {
            float score =
                CosineSimilarityCalculator.Calculate(
                    questionEmbedding,
                    candidate.Embedding);

            if (!bestCandidateByKnowledgeItem.TryGetValue(
                    candidate.KnowledgeItemId,
                    out ScoredCandidate? currentBest) ||
                score > currentBest.Score)
            {
                bestCandidateByKnowledgeItem[
                    candidate.KnowledgeItemId] =
                    new ScoredCandidate
                    {
                        Candidate = candidate,
                        Score = score
                    };
            }
        }

        ScoredCandidate[] rankedResults =
            bestCandidateByKnowledgeItem.Values
                .OrderByDescending(result => result.Score)
                .ToArray();

        ScoredCandidate best = rankedResults[0];

        ScoredCandidate? secondBest =
            rankedResults.Length > 1
                ? rankedResults[1]
                : null;

        float? margin =
            secondBest is null
                ? null
                : best.Score - secondBest.Score;

        return new ChatbotSemanticSearchResult
        {
            KnowledgeItemId =
                best.Candidate.KnowledgeItemId,

            Answer =
                best.Candidate.Answer,

            MatchedText =
                best.Candidate.Text,

            Score =
                best.Score,

            SecondBestKnowledgeItemId =
                secondBest?.Candidate.KnowledgeItemId,

            SecondBestScore =
                secondBest?.Score,

            Margin =
                margin
        };
    }

    private sealed class ScoredCandidate
    {
        public required ChatbotSemanticCandidate Candidate
        {
            get;
            init;
        }

        public required float Score { get; init; }
    }
}