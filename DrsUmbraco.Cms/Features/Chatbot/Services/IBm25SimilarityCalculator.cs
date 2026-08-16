namespace DrsUmbraco.Cms.Features.Chatbot.Services;

public interface IBm25SimilarityCalculator
{
    float Calculate(
        string query,
        string document,
        Bm25CorpusStatistics statistics);
}