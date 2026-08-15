using DrsUmbraco.Cms.Features.Chatbot.Text;

namespace DrsUmbraco.Cms.Features.Chatbot.Search;

public sealed class PersianLexicalSimilarityCalculator
    : ILexicalSimilarityCalculator
{
    private const int NGramSize = 3;

    private readonly IPersianTextNormalizer _normalizer;

    public PersianLexicalSimilarityCalculator(
        IPersianTextNormalizer normalizer)
    {
        _normalizer = normalizer;
    }

    public float Calculate(
        string? firstText,
        string? secondText)
    {
        string first = _normalizer.Normalize(firstText);

        string second = _normalizer.Normalize(secondText);

        if (string.IsNullOrWhiteSpace(first) ||
            string.IsNullOrWhiteSpace(second))
        {
            return 0f;
        }

        if (string.Equals(
            first,
            second,
            StringComparison.Ordinal))
        {
            return 1f;
        }

        HashSet<string> firstNGrams = CreateNGrams(first);

        HashSet<string> secondNGrams = CreateNGrams(second);

        if (firstNGrams.Count == 0 ||
            secondNGrams.Count == 0)
        {
            return 0f;
        }

        int intersectionCount = firstNGrams.Count(secondNGrams.Contains);

        return
            (2f * intersectionCount) /
            (firstNGrams.Count +
             secondNGrams.Count);
    }

    private static HashSet<string> CreateNGrams(
        string text)
    {
        HashSet<string> ngrams = new(StringComparer.Ordinal);

        string[] words =
            text.Split(
                ' ',
                StringSplitOptions.RemoveEmptyEntries);

        foreach (string word in words)
        {
            if (word.Length <= NGramSize)
            {
                ngrams.Add(word);
                continue;
            }

            for (int i = 0; i <= word.Length - NGramSize; i++)
            {
                ngrams.Add(
                    word.Substring(
                        i,
                        NGramSize));
            }
        }

        return ngrams;
    }
}