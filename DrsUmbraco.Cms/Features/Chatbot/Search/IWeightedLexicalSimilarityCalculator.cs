namespace DrsUmbraco.Cms.Features.Chatbot.Search;

public interface IWeightedLexicalSimilarityCalculator
{
    float Calculate(
        string? firstText,
        string? secondText,
        LexicalCorpusStatistics statistics);
}