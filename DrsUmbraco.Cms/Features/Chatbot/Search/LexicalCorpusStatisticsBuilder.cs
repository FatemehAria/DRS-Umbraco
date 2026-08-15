using DrsUmbraco.Cms.Features.Chatbot.Embeddings;

namespace DrsUmbraco.Cms.Features.Chatbot.Search;

public sealed class LexicalCorpusStatisticsBuilder
    : ILexicalCorpusStatisticsBuilder
{
    private readonly ICharacterNGramExtractor _ngramExtractor;

    public LexicalCorpusStatisticsBuilder(
        ICharacterNGramExtractor ngramExtractor)
    {
        _ngramExtractor = ngramExtractor;
    }

    public LexicalCorpusStatistics Build(
        IReadOnlyList<ChatbotSemanticCandidate> candidates)
    {
        Dictionary<string, int> documentFrequencies =
            new(StringComparer.Ordinal);

        foreach (ChatbotSemanticCandidate candidate in candidates)
        {
            IReadOnlySet<string> ngrams =
                _ngramExtractor.Extract(
                    candidate.Text);

            foreach (string ngram in ngrams)
            {
                if (documentFrequencies.TryGetValue(
                        ngram,
                        out int currentCount))
                {
                    documentFrequencies[ngram] =
                        currentCount + 1;
                }
                else
                {
                    documentFrequencies[ngram] = 1;
                }
            }
        }

        int documentCount = candidates.Count;

        Dictionary<string, float> idfWeights =
            new(StringComparer.Ordinal);

        foreach (
            KeyValuePair<string, int> item
            in documentFrequencies)
        {
            float idf =
                CalculateIdf(
                    documentCount,
                    item.Value);

            idfWeights[item.Key] = idf;
        }

        return new LexicalCorpusStatistics
        {
            IdfWeights = idfWeights
        };
    }

    private static float CalculateIdf(
        int documentCount,
        int documentFrequency)
    {
        return (float)(
            Math.Log(
                (documentCount + 1.0) /
                (documentFrequency + 1.0))
            + 1.0);
    }
}