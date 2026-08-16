using DrsUmbraco.Cms.Features.Chatbot.Services;
using Microsoft.Extensions.Options;

namespace DrsUmbraco.Cms.Tests.Features.Chatbot.Services;

public sealed class ChatbotNoMatchDecisionServiceTests
{
    [Fact]
    public void Decide_WhenScoreIsBelowMinimum_ShouldReturnNoMatch()
    {
        ChatbotNoMatchDecisionService service =
            CreateService(0.85f);

        ChatbotNoMatchDecision result =
            service.Decide(0.81f);

        Assert.Equal(
            ChatbotNoMatchDecision.NoMatch,
            result);
    }

    [Fact]
    public void Decide_WhenScoreEqualsMinimum_ShouldReturnInDomain()
    {
        ChatbotNoMatchDecisionService service =
            CreateService(0.85f);

        ChatbotNoMatchDecision result =
            service.Decide(0.85f);

        Assert.Equal(
            ChatbotNoMatchDecision.InDomain,
            result);
    }

    [Fact]
    public void Decide_WhenScoreIsAboveMinimum_ShouldReturnInDomain()
    {
        ChatbotNoMatchDecisionService service =
            CreateService(0.85f);

        ChatbotNoMatchDecision result =
            service.Decide(0.92f);

        Assert.Equal(
            ChatbotNoMatchDecision.InDomain,
            result);
    }

    private static ChatbotNoMatchDecisionService
        CreateService(float minimumScore)
    {
        ChatbotNoMatchDecisionOptions options =
            new()
            {
                MinimumSemanticScore =
                    minimumScore
            };

        IOptions<ChatbotNoMatchDecisionOptions> wrappedOptions =
                    new OptionsWrapper<ChatbotNoMatchDecisionOptions>(options);

        return new ChatbotNoMatchDecisionService(wrappedOptions);
    }
}