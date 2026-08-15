namespace DrsUmbraco.Cms.Features.Chatbot.Search;

public interface ILexicalSimilarityCalculator
{
    float Calculate(
        string? firstText,
        string? secondText);
}