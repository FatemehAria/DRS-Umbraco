using DrsUmbraco.Cms.Features.Chatbot.Embeddings;
using DrsUmbraco.Cms.Features.Chatbot.Models;
using DrsUmbraco.Cms.Features.Chatbot.Search;

public sealed class ChatbotWeightedHybridSearchService
    : IChatbotWeightedHybridSearchService
{
    private const float SemanticWeight = 0.85f;
    private const float LexicalWeight = 0.15f;

    private readonly IEmbeddingService _embeddingService;
    private readonly IChatbotSemanticIndex _semanticIndex;

    private readonly ILexicalCorpusStatisticsBuilder
        _statisticsBuilder;

    private readonly IWeightedLexicalSimilarityCalculator
        _lexicalCalculator;

    public ChatbotWeightedHybridSearchService(
        IEmbeddingService embeddingService,
        IChatbotSemanticIndex semanticIndex,
        ILexicalCorpusStatisticsBuilder statisticsBuilder,
        IWeightedLexicalSimilarityCalculator lexicalCalculator)
    {
        _embeddingService = embeddingService;
        _semanticIndex = semanticIndex;
        _statisticsBuilder = statisticsBuilder;
        _lexicalCalculator = lexicalCalculator;
    }

    public ChatbotHybridSearchResult? FindBest(
        string question)
    {
        if (string.IsNullOrWhiteSpace(question))
        {
            return null;
        }

        IReadOnlyList<ChatbotSemanticCandidate> candidates =
            _semanticIndex.GetAll();

        if (candidates.Count == 0)
        {
            return null;
        }

        float[] questionEmbedding =
            _embeddingService.Generate(question);

        LexicalCorpusStatistics statistics =
            _statisticsBuilder.Build(candidates);

        Dictionary<Guid, ScoredCandidate>
            bestByKnowledgeItem = [];

        foreach (ChatbotSemanticCandidate candidate in candidates)
        {
            float semanticScore =
                CosineSimilarityCalculator.Calculate(
                    questionEmbedding,
                    candidate.Embedding);

            float lexicalScore =
                _lexicalCalculator.Calculate(
                    question,
                    candidate.Text,
                    statistics);

            float hybridScore =
                (semanticScore * SemanticWeight) +
                (lexicalScore * LexicalWeight);

            ScoredCandidate scored =
                new()
                {
                    Candidate = candidate,
                    SemanticScore = semanticScore,
                    LexicalScore = lexicalScore,
                    HybridScore = hybridScore
                };

            if (!bestByKnowledgeItem.TryGetValue(
                    candidate.KnowledgeItemId,
                    out ScoredCandidate? existing) ||
                scored.HybridScore > existing.HybridScore)
            {
                bestByKnowledgeItem[
                    candidate.KnowledgeItemId] = scored;
            }
        }

        ScoredCandidate[] ranked =
            bestByKnowledgeItem
                .Values
                .OrderByDescending(
                    item => item.HybridScore)
                .ToArray();

        if (ranked.Length == 0)
        {
            return null;
        }

        ScoredCandidate best = ranked[0];

        ScoredCandidate? second =
            ranked.Length > 1
                ? ranked[1]
                : null;

        return new ChatbotHybridSearchResult
        {
            KnowledgeItemId =
                best.Candidate.KnowledgeItemId,

            Answer =
                best.Candidate.Answer,

            MatchedText =
                best.Candidate.Text,

            SemanticScore =
                best.SemanticScore,

            LexicalScore =
                best.LexicalScore,

            Score =
                best.HybridScore,

            SecondBestKnowledgeItemId =
                second?.Candidate.KnowledgeItemId,

            SecondBestScore =
                second?.HybridScore,

            Margin =
                second is null
                    ? null
                    : best.HybridScore -
                      second.HybridScore
        };
    }

    private sealed class ScoredCandidate
    {
        public required ChatbotSemanticCandidate Candidate
        {
            get;
            init;
        }

        public required float SemanticScore
        {
            get;
            init;
        }

        public required float LexicalScore
        {
            get;
            init;
        }

        public required float HybridScore
        {
            get;
            init;
        }
    }
}