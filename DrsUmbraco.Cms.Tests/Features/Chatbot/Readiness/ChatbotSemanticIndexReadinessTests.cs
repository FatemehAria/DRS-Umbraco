using DrsUmbraco.Cms.Features.Chatbot.Readiness;

namespace DrsUmbraco.Cms.Tests.Features.Chatbot.Readiness;

public sealed class ChatbotSemanticIndexReadinessTests
{
    [Fact]
    public void IsReady_WhenCreated_ShouldBeFalse()
    {
        ChatbotSemanticIndexReadiness readiness =
            new();

        Assert.False(
            readiness.IsReady);
    }

    [Fact]
    public void MarkReady_ShouldMakeStateReady()
    {
        ChatbotSemanticIndexReadiness readiness =
            new();

        readiness.MarkReady();

        Assert.True(
            readiness.IsReady);
    }

    [Fact]
    public void MarkUnavailable_AfterMarkReady_ShouldMakeStateUnavailable()
    {
        ChatbotSemanticIndexReadiness readiness =
            new();

        readiness.MarkReady();

        readiness.MarkUnavailable();

        Assert.False(
            readiness.IsReady);
    }
}