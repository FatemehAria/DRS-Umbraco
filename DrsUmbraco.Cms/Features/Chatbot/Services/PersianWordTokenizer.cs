using DrsUmbraco.Cms.Features.Chatbot.Text;

namespace DrsUmbraco.Cms.Features.Chatbot.Services;

public sealed class PersianWordTokenizer
    : IPersianWordTokenizer
{
    private readonly IPersianTextNormalizer _normalizer;

    public PersianWordTokenizer(
        IPersianTextNormalizer normalizer)
    {
        _normalizer = normalizer;
    }

    public IReadOnlyList<string> Tokenize(string? text)
    {
        string normalized =
            _normalizer.Normalize(text);

        if (string.IsNullOrWhiteSpace(normalized))
        {
            return [];
        }

        return normalized.Split(
            ' ',
            StringSplitOptions.RemoveEmptyEntries |
            StringSplitOptions.TrimEntries);
    }
}