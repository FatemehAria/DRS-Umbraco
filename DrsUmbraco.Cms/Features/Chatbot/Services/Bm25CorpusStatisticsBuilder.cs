namespace DrsUmbraco.Cms.Features.Chatbot.Services;

public sealed class Bm25CorpusStatisticsBuilder
    : IBm25CorpusStatisticsBuilder
{
    private readonly IPersianWordTokenizer _tokenizer;
    private readonly IBm25TermFilter _termFilter;

    public Bm25CorpusStatisticsBuilder(
        IPersianWordTokenizer tokenizer,
        IBm25TermFilter termFilter)
    {
        _tokenizer = tokenizer;
        _termFilter = termFilter;
    }

    public Bm25CorpusStatistics Build(
        IReadOnlyList<string> documents)
    {
        ArgumentNullException.ThrowIfNull(documents);

        if (documents.Count == 0)
        {
            return new Bm25CorpusStatistics
            {
                DocumentCount = 0,
                AverageDocumentLength = 0f,
                DocumentFrequencies =
                    new Dictionary<string, int>()
            };
        }

        Dictionary<string, int> documentFrequencies =
            new(StringComparer.Ordinal);

        int totalDocumentLength = 0;

        foreach (string document in documents)
        {
            IReadOnlyList<string> tokens = _termFilter.Filter(_tokenizer.Tokenize(document));

            totalDocumentLength += tokens.Count;

            foreach (string token in tokens.Distinct(
                         StringComparer.Ordinal))
            {
                if (documentFrequencies.TryGetValue(
                        token,
                        out int currentCount))
                {
                    documentFrequencies[token] =
                        currentCount + 1;
                }
                else
                {
                    documentFrequencies[token] = 1;
                }
            }
        }

        float averageDocumentLength =
            totalDocumentLength /
            (float)documents.Count;

        return new Bm25CorpusStatistics
        {
            DocumentCount = documents.Count,
            AverageDocumentLength =
                averageDocumentLength,
            DocumentFrequencies =
                documentFrequencies
        };
    }
}