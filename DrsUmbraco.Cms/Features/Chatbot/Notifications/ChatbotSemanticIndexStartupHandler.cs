using DrsUmbraco.Cms.Features.Chatbot.Embeddings;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;
using System.Diagnostics;

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

        Stopwatch semanticIndexStopwatch = Stopwatch.StartNew();

        try
        {
            using IServiceScope scope =
                _scopeFactory.CreateScope();

            IChatbotSemanticIndexBuilder indexBuilder =
                scope.ServiceProvider
                    .GetRequiredService<IChatbotSemanticIndexBuilder>();

            int candidateCount = indexBuilder.Rebuild();

            semanticIndexStopwatch.Stop();

            _logger.LogInformation(
                "Performance metric {MetricName} completed in {ElapsedMs} ms with {CandidateCount} candidates.",
                "SemanticIndexBuild",
                semanticIndexStopwatch.ElapsedMilliseconds,
                candidateCount);
        }
        catch (Exception exception)
        {
            semanticIndexStopwatch.Stop();

            _logger.LogError(
                exception,
                "Performance metric {MetricName} failed after {ElapsedMs} ms.",
                "SemanticIndexBuild",
                semanticIndexStopwatch.ElapsedMilliseconds);
        }
    }
}