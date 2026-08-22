namespace DrsUmbraco.Cms.Features.Chatbot.Relevance;

public sealed class BgeRelevanceOptions
{
    public const string SectionName =
        "Chatbot:RelevanceVerifier";

    public required string ModelPath { get; init; }

    public required string TokenizerPath { get; init; }

    public float Threshold { get; init; }
}