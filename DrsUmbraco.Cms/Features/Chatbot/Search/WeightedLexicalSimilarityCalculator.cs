namespace DrsUmbraco.Cms.Features.Chatbot.Search;

public sealed class WeightedLexicalSimilarityCalculator
    : IWeightedLexicalSimilarityCalculator
{
    private readonly ICharacterNGramExtractor _ngramExtractor;

    public WeightedLexicalSimilarityCalculator(
        ICharacterNGramExtractor ngramExtractor)
    {
        _ngramExtractor = ngramExtractor;
    }

    public float Calculate(
        string? firstText,
        string? secondText,
        LexicalCorpusStatistics statistics)
    {
        ArgumentNullException.ThrowIfNull(statistics);

        IReadOnlySet<string> firstNGrams =
            _ngramExtractor.Extract(firstText);

        IReadOnlySet<string> secondNGrams =
            _ngramExtractor.Extract(secondText);

        if (firstNGrams.Count == 0 ||
            secondNGrams.Count == 0)
        {
            return 0f;
        }

        float firstWeight =
            CalculateTotalWeight(
                firstNGrams,
                statistics);

        float secondWeight =
            CalculateTotalWeight(
                secondNGrams,
                statistics);

        float intersectionWeight = 0f;

        foreach (string ngram in firstNGrams)
        {
            if (secondNGrams.Contains(ngram))
            {
                intersectionWeight +=
                    GetWeight(
                        ngram,
                        statistics);
            }
        }

        float totalWeight =
            firstWeight + secondWeight;

        if (totalWeight == 0f)
        {
            return 0f;
        }

        return
            (2f * intersectionWeight) /
            totalWeight;
    }

    private static float CalculateTotalWeight(
        IReadOnlySet<string> ngrams,
        LexicalCorpusStatistics statistics)
    {
        float total = 0f;

        foreach (string ngram in ngrams)
        {
            total +=
                GetWeight(
                    ngram,
                    statistics);
        }

        return total;
    }

    private static float GetWeight(
        string ngram,
        LexicalCorpusStatistics statistics)
    {
        return statistics.IdfWeights.TryGetValue(
            ngram,
            out float weight)
                ? weight
                : statistics.UnseenIdfWeight;
    }
}