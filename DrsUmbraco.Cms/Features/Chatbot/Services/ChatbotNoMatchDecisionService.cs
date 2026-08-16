using Microsoft.Extensions.Options;

namespace DrsUmbraco.Cms.Features.Chatbot.Services;

public sealed class ChatbotNoMatchDecisionService
    : IChatbotNoMatchDecisionService
{
    private readonly ChatbotNoMatchDecisionOptions
        _options;

    public ChatbotNoMatchDecisionService(
        IOptions<ChatbotNoMatchDecisionOptions> options)
    {
        _options = options.Value;
    }

    public ChatbotNoMatchDecision Decide(
        float semanticTopScore)
    {
        if (semanticTopScore <
            _options.MinimumSemanticScore)
        {
            return ChatbotNoMatchDecision.NoMatch;
        }

        return ChatbotNoMatchDecision.InDomain;
    }
}