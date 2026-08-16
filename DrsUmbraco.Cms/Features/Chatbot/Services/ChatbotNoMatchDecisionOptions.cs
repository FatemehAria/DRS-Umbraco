namespace DrsUmbraco.Cms.Features.Chatbot.Services;

public sealed class ChatbotNoMatchDecisionOptions
{
    public const string SectionName =
        "Chatbot:NoMatchDecision";

    public float MinimumSemanticScore { get; init; }
        = 0.85f;
}