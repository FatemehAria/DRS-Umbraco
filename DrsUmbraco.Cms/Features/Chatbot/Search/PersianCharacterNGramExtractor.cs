using DrsUmbraco.Cms.Features.Chatbot.Text;

namespace DrsUmbraco.Cms.Features.Chatbot.Search;

public sealed class PersianCharacterNGramExtractor
    : ICharacterNGramExtractor
{
    private const int NGramSize = 3;

    private readonly IPersianTextNormalizer _normalizer;

    public PersianCharacterNGramExtractor(
        IPersianTextNormalizer normalizer)
    {
        _normalizer = normalizer;
    }

    public IReadOnlySet<string> Extract(string? text)
    {
        string normalizedText =
            _normalizer.Normalize(text);

        HashSet<string> ngrams =
            new(StringComparer.Ordinal);

        if (string.IsNullOrWhiteSpace(normalizedText))
        {
            return ngrams;
        }

        string[] words =
            normalizedText.Split(
                ' ',
                StringSplitOptions.RemoveEmptyEntries);

        foreach (string word in words)
        {
            if (word.Length <= NGramSize)
            {
                ngrams.Add(word);
                continue;
            }

            for (int i = 0;
                 i <= word.Length - NGramSize;
                 i++)
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