using DrsUmbraco.Cms.Features.Chatbot.Embeddings;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;

namespace DrsUmbraco.Cms.Features.Chatbot.Notifications;

public sealed class ChatbotSemanticIndexStartupHandler
    : INotificationHandler<UmbracoApplicationStartedNotification>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IRuntimeState _runtimeState;
    private readonly ILogger<ChatbotSemanticIndexStartupHandler> _logger;

    public ChatbotSemanticIndexStartupHandler(
        IServiceScopeFactory scopeFactory,
        IRuntimeState runtimeState,
        ILogger<ChatbotSemanticIndexStartupHandler> logger)
    {
        _scopeFactory = scopeFactory;
        _runtimeState = runtimeState;
        _logger = logger;
    }

    public void Handle(
        UmbracoApplicationStartedNotification notification)
    {
        if (_runtimeState.Level != RuntimeLevel.Run)
        {
            return;
        }

        try
        {
            using IServiceScope scope =
                _scopeFactory.CreateScope();

            IChatbotSemanticIndexBuilder indexBuilder =
                scope.ServiceProvider
                    .GetRequiredService<IChatbotSemanticIndexBuilder>();

            int candidateCount =
                indexBuilder.Rebuild();

            _logger.LogInformation(
                "Chatbot semantic index built at startup with {CandidateCount} candidates.",
                candidateCount);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to build chatbot semantic index at startup.");
        }
    }
}