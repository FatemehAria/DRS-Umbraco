namespace DrsUmbraco.Cms.Features.Chatbot.Search;

public sealed class LexicalCorpusStatistics
{
    public required IReadOnlyDictionary<string, float> IdfWeights
    {
        get;
        init;
    }

    public required float UnseenIdfWeight
    {
        get;
        init;
    }
}