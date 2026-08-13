using DrsUmbraco.Cms.Features.Chatbot.Embeddings;
using DrsUmbraco.Cms.Features.Chatbot.Configuration;
using Microsoft.Extensions.Options;

namespace DrsUmbraco.Cms.Tests.Features.Chatbot.Embeddings;

public sealed class ChatbotSemanticDecisionServiceTests
{

    [Fact]
    public void Decide_WhenResultIsNull_ShouldReturnNoMatch()
    {
        ChatbotSemanticDecisionService service = CreateService();

        ChatbotSemanticDecision decision =
            service.Decide(null);

        Assert.Equal(
            ChatbotSemanticDecisionType.NoMatch,
            decision.Type);
    }

    [Fact]
    public void Decide_WhenScoreIsBelowMinimum_ShouldReturnNoMatch()
    {
        ChatbotSemanticSearchResult result =
            CreateResult(
                score: 0.80f,
                margin: 0.10f);

        ChatbotSemanticDecisionService service = CreateService();

        ChatbotSemanticDecision decision =
            service.Decide(result);

        Assert.Equal(
            ChatbotSemanticDecisionType.NoMatch,
            decision.Type);
    }

    [Fact]
    public void Decide_WhenScoreIsHighButMarginIsSmall_ShouldReturnAmbiguous()
    {
        ChatbotSemanticSearchResult result =
            CreateResult(
                score: 0.92f,
                margin: 0.02f);

        ChatbotSemanticDecisionService service = CreateService();

        ChatbotSemanticDecision decision =
            service.Decide(result);

        Assert.Equal(
            ChatbotSemanticDecisionType.Ambiguous,
            decision.Type);
    }

    [Fact]
    public void Decide_WhenScoreAndMarginAreHighEnough_ShouldReturnConfident()
    {
        ChatbotSemanticSearchResult result =
            CreateResult(
                score: 0.95f,
                margin: 0.10f);

        ChatbotSemanticDecisionService service = CreateService();

        ChatbotSemanticDecision decision =
            service.Decide(result);

        Assert.Equal(
            ChatbotSemanticDecisionType.Confident,
            decision.Type);
    }

    [Fact]
    public void Decide_ShouldUseConfiguredMinimumScore()
    {
        ChatbotSemanticDecisionService service =
            CreateService(
                minimumScore: 0.95f,
                minimumMargin: 0.05f);

        ChatbotSemanticSearchResult result =
            CreateResult(
                score: 0.92f,
                margin: 0.10f);

        ChatbotSemanticDecision decision =
            service.Decide(result);

        Assert.Equal(
            ChatbotSemanticDecisionType.NoMatch,
            decision.Type);
    }
    
    private static ChatbotSemanticDecisionService CreateService(
    float minimumScore = 0.88f,
    float minimumMargin = 0.05f)
    {
        ChatbotSemanticDecisionOptions options =
            new()
            {
                MinimumScore = minimumScore,
                MinimumMargin = minimumMargin
            };

        IOptions<ChatbotSemanticDecisionOptions> wrappedOptions =
            new OptionsWrapper<ChatbotSemanticDecisionOptions>(options);

        return new ChatbotSemanticDecisionService(
            wrappedOptions);
    }

    private static ChatbotSemanticSearchResult CreateResult(
        float score,
        float? margin)
    {
        return new ChatbotSemanticSearchResult
        {
            KnowledgeItemId = Guid.NewGuid(),
            Answer = "Test answer",
            MatchedText = "Test question",
            Score = score,
            Margin = margin
        };
    }
}