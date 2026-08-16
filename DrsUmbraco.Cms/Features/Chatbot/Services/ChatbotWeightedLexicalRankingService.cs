using DrsUmbraco.Cms.Features.Chatbot.Embeddings;
using DrsUmbraco.Cms.Features.Chatbot.Search;

namespace DrsUmbraco.Cms.Features.Chatbot.Services;

public sealed class ChatbotWeightedLexicalRankingService
    : IChatbotWeightedLexicalRankingService
{
    private readonly IChatbotSemanticIndex _semanticIndex;
    private readonly ILexicalCorpusStatisticsBuilder _statisticsBuilder;
    private readonly IWeightedLexicalSimilarityCalculator _lexicalCalculator;

    public ChatbotWeightedLexicalRankingService(
        IChatbotSemanticIndex semanticIndex,
        ILexicalCorpusStatisticsBuilder statisticsBuilder,
        IWeightedLexicalSimilarityCalculator lexicalCalculator)
    {
        _semanticIndex = semanticIndex;
        _statisticsBuilder = statisticsBuilder;
        _lexicalCalculator = lexicalCalculator;
    }

    public IReadOnlyList<ChatbotLexicalRankedResult> FindTop(
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

        LexicalCorpusStatistics statistics =
            _statisticsBuilder.Build(candidates);

        Dictionary<Guid, ScoredCandidate> bestByKnowledgeItem = [];

        foreach (ChatbotSemanticCandidate candidate in candidates)
        {
            float score =
                _lexicalCalculator.Calculate(
                    question,
                    candidate.Text,
                    statistics);

            ScoredCandidate scored =
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
                    candidate.KnowledgeItemId] = scored;
            }
        }

        return bestByKnowledgeItem
                .Values
                .Where(item => item.Score > 0f)
                .OrderByDescending(item => item.Score)
                .Take(limit)
                .Select(
                    (item, index) =>
                        new ChatbotLexicalRankedResult
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