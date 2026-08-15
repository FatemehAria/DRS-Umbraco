namespace DrsUmbraco.Cms.Features.Chatbot.Search;

public sealed class PersianLexicalSimilarityCalculator
    : ILexicalSimilarityCalculator
{
    private readonly ICharacterNGramExtractor _ngramExtractor;

    public PersianLexicalSimilarityCalculator(
        ICharacterNGramExtractor ngramExtractor)
    {
        _ngramExtractor = ngramExtractor;
    }

    public float Calculate(
        string? firstText,
        string? secondText)
    {
        IReadOnlySet<string> firstNGrams =
            _ngramExtractor.Extract(firstText);

        IReadOnlySet<string> secondNGrams =
            _ngramExtractor.Extract(secondText);

        if (firstNGrams.Count == 0 ||
            secondNGrams.Count == 0)
        {
            return 0f;
        }

        if (firstNGrams.SetEquals(secondNGrams))
        {
            return 1f;
        }

        int intersectionCount =
            firstNGrams.Count(
                secondNGrams.Contains);

        return
            (2f * intersectionCount) /
            (firstNGrams.Count +
             secondNGrams.Count);
    }
}