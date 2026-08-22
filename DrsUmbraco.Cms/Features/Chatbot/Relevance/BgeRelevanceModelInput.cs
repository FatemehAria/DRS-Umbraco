namespace DrsUmbraco.Cms.Features.Chatbot.Relevance;

public sealed class BgeRelevanceModelInput
{
    public required long[] InputIds { get; init; }

    public required long[] AttentionMask { get; init; }
}