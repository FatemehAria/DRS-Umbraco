using DrsUmbraco.Cms.Features.Chatbot.Configuration;
using Microsoft.Extensions.Options;

namespace DrsUmbraco.Cms.Features.Chatbot.Embeddings;

public sealed class ChatbotSemanticDecisionService
    : IChatbotSemanticDecisionService
{
    private readonly ChatbotSemanticDecisionOptions _options;

    public ChatbotSemanticDecisionService(
        IOptions<ChatbotSemanticDecisionOptions> options)
    {
        _options = options.Value;
    }


    public ChatbotSemanticDecision Decide(
        ChatbotSemanticSearchResult? result)
    {
        if (result is null)
        {
            return new ChatbotSemanticDecision
            {
                Type = ChatbotSemanticDecisionType.NoMatch
            };
        }

        if (result.Score < _options.MinimumScore)
        {
            return new ChatbotSemanticDecision
            {
                Type = ChatbotSemanticDecisionType.NoMatch,
                SearchResult = result
            };
        }

        if (result.Margin is not null &&
            result.Margin < _options.MinimumMargin)
        {
            return new ChatbotSemanticDecision
            {
                Type = ChatbotSemanticDecisionType.Ambiguous,
                SearchResult = result
            };
        }

        return new ChatbotSemanticDecision
        {
            Type = ChatbotSemanticDecisionType.Confident,
            SearchResult = result
        };
    }
}