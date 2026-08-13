namespace DrsUmbraco.Cms.Features.Chatbot.Configuration;

public sealed class ChatbotSemanticDecisionOptions
{
    public const string SectionName =
        "Chatbot:SemanticDecision";

    public float MinimumScore { get; init; }

    public float MinimumMargin { get; init; }
}