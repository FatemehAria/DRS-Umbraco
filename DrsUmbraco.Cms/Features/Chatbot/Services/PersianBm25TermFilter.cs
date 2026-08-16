namespace DrsUmbraco.Cms.Features.Chatbot.Services;

public sealed class PersianBm25TermFilter
    : IBm25TermFilter
{
    private static readonly HashSet<string> StopWords =
        new(
            [
                "از",
                "به",
                "در",
                "با",
                "برای",
                "و",
                "یا",
                "که",
                "را",
                "رو",
                "تو",
                "این",
                "اون",
                "آن",
                "یک",
                "یه",
                "من",
                "ما",
                "شما",
                "چطور",
                "چجوری",
                "کجا",
                "کجاست",
                "چی",
                "چیکار",
                "کنم",
                "باید"
            ],
            StringComparer.Ordinal);

    public IReadOnlyList<string> Filter(
        IReadOnlyList<string> tokens)
    {
        ArgumentNullException.ThrowIfNull(tokens);

        return tokens
            .Where(token =>
                !StopWords.Contains(token))
            .ToArray();
    }
}