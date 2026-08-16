namespace DrsUmbraco.Cms.Features.Chatbot.Services;

public sealed class Bm25CorpusStatistics
{
    public required int DocumentCount { get; init; }

    public required float AverageDocumentLength { get; init; }

    public required IReadOnlyDictionary<string, int>
        DocumentFrequencies
    { get; init; }
}