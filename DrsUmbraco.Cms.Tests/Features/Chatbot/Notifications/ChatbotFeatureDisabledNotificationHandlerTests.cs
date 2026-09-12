using DrsUmbraco.Cms.Features.Chatbot.Configuration;
using DrsUmbraco.Cms.Features.Chatbot.Notifications;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using MsOptions =
    Microsoft.Extensions.Options.Options;

namespace DrsUmbraco.Cms.Tests.Features.Chatbot.Notifications;

public sealed class
    ChatbotFeatureDisabledNotificationHandlerTests
{
    [Fact]
    public void StartupHandler_WhenFeatureIsDisabled_ShouldNotCreateScope()
    {
        RecordingScopeFactory scopeFactory = new();

        ChatbotSemanticIndexStartupHandler handler =
            new(
                scopeFactory,
                runtimeState: null!,
                readiness: null!,
                MsOptions.Create(
                    new ChatbotFeatureOptions
                    {
                        Enabled = false
                    }),
                NullLogger<ChatbotSemanticIndexStartupHandler>.Instance);

        handler.Handle(notification: null!);

        Assert.False(scopeFactory.WasCreateScopeCalled);
    }

    [Fact]
    public void CacheHandler_WhenFeatureIsDisabled_ShouldNotCreateScope()
    {
        RecordingScopeFactory scopeFactory = new();

        ChatbotSemanticIndexCacheHandler handler =
            new(
                scopeFactory,
                MsOptions.Create(
                    new ChatbotFeatureOptions
                    {
                        Enabled = false
                    }),
                NullLogger<ChatbotSemanticIndexCacheHandler>.Instance);

        handler.Handle(notification: null!);

        Assert.False(scopeFactory.WasCreateScopeCalled);
    }

    private sealed class RecordingScopeFactory
        : IServiceScopeFactory
    {
        public bool WasCreateScopeCalled
        {
            get;
            private set;
        }

        public IServiceScope CreateScope()
        {
            WasCreateScopeCalled = true;

            throw new InvalidOperationException("A scope must not be created when the chatbot feature is disabled.");
        }
    }
}