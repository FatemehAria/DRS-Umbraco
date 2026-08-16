namespace DrsUmbraco.Cms.Features.Chatbot.Services;

public sealed class Bm25SimilarityCalculator
    : IBm25SimilarityCalculator
{
    private const float K1 = 1.2f;
    private const float B = 0.75f;

    private readonly IPersianWordTokenizer _tokenizer;

    public Bm25SimilarityCalculator(
        IPersianWordTokenizer tokenizer)
    {
        _tokenizer = tokenizer;
    }

    public float Calculate(
        string query,
        string document,
        Bm25CorpusStatistics statistics)
    {
        ArgumentNullException.ThrowIfNull(statistics);

        if (statistics.DocumentCount == 0 ||
            statistics.AverageDocumentLength <= 0f)
        {
            return 0f;
        }

        IReadOnlyList<string> queryTokens =
            _tokenizer.Tokenize(query);

        IReadOnlyList<string> documentTokens =
            _tokenizer.Tokenize(document);

        if (queryTokens.Count == 0 ||
            documentTokens.Count == 0)
        {
            return 0f;
        }

        Dictionary<string, int> termFrequencies =
            documentTokens
                .GroupBy(
                    token => token,
                    StringComparer.Ordinal)
                .ToDictionary(
                    group => group.Key,
                    group => group.Count(),
                    StringComparer.Ordinal);

        float score = 0f;

        foreach (string queryTerm in queryTokens.Distinct(
                     StringComparer.Ordinal))
        {
            if (!termFrequencies.TryGetValue(
                    queryTerm,
                    out int termFrequency))
            {
                continue;
            }

            if (!statistics.DocumentFrequencies.TryGetValue(
                    queryTerm,
                    out int documentFrequency))
            {
                continue;
            }

            float idf = CalculateIdf(
                statistics.DocumentCount,
                documentFrequency);

            float documentLength =
                documentTokens.Count;

            float normalization =
                K1 *
                (
                    1f - B +
                    B *
                    (
                        documentLength /
                        statistics.AverageDocumentLength
                    )
                );

            float termScore =
                idf *
                (
                    termFrequency * (K1 + 1f)
                ) /
                (
                    termFrequency +
                    normalization
                );

            score += termScore;
        }

        return score;
    }

    private static float CalculateIdf(
        int documentCount,
        int documentFrequency)
    {
        return MathF.Log(
            1f +
            (
                documentCount -
                documentFrequency +
                0.5f
            ) /
            (
                documentFrequency +
                0.5f
            ));
    }
}