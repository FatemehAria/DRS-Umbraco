namespace DrsUmbraco.Cms.Features.Chatbot.Services;

public interface IBm25CorpusStatisticsBuilder
{
    Bm25CorpusStatistics Build(IReadOnlyList<string> documents);
}