using DrsUmbraco.Cms.Features.Chatbot.Embeddings;
using DrsUmbraco.Cms.Features.Chatbot.Search;

namespace DrsUmbraco.Cms.Features.Chatbot.Services;

public sealed class ChatbotBm25RankingService
    : IChatbotBm25RankingService
{
    private readonly IChatbotSemanticIndex _semanticIndex;
    private readonly IBm25CorpusStatisticsBuilder
        _statisticsBuilder;
    private readonly IBm25SimilarityCalculator
        _bm25Calculator;

    public ChatbotBm25RankingService(
        IChatbotSemanticIndex semanticIndex,
        IBm25CorpusStatisticsBuilder statisticsBuilder,
        IBm25SimilarityCalculator bm25Calculator)
    {
        _semanticIndex = semanticIndex;
        _statisticsBuilder = statisticsBuilder;
        _bm25Calculator = bm25Calculator;
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

        string[] documents =
            candidates
                .Select(candidate => candidate.Text)
                .ToArray();

        Bm25CorpusStatistics statistics =
            _statisticsBuilder.Build(documents);

        var bestPerKnowledgeItem =
            candidates
                .Select(candidate => new
                {
                    Candidate = candidate,

                    Score = _bm25Calculator.Calculate(
                        question,
                        candidate.Text,
                        statistics)
                })
                .GroupBy(
                    item => item.Candidate.KnowledgeItemId)
                .Select(group =>
                    group
                        .OrderByDescending(item => item.Score)
                        .First())
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

        return bestPerKnowledgeItem;
    }
}