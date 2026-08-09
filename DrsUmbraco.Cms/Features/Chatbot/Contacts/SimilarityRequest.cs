namespace DrsUmbraco.Cms.Features.Chatbot.Contracts;

public sealed class SimilarityRequest
{
    public string? FirstText { get; init; }

    public string? SecondText { get; init; }
}