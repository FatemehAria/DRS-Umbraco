using DrsUmbraco.Cms.Features.Chatbot.Embeddings;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;
using System.Diagnostics;
using DrsUmbraco.Cms.Features.Chatbot.Readiness;

namespace DrsUmbraco.Cms.Features.Chatbot.Notifications;

public sealed class ChatbotSemanticIndexStartupHandler
    : INotificationHandler<UmbracoApplicationStartedNotification>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IRuntimeState _runtimeState;
    private readonly ILogger<ChatbotSemanticIndexStartupHandler> _logger;
    private readonly IChatbotSemanticIndexReadiness _readiness;
    public ChatbotSemanticIndexStartupHandler(
        IServiceScopeFactory scopeFactory,
        IRuntimeState runtimeState,
        IChatbotSemanticIndexReadiness readiness,
        ILogger<ChatbotSemanticIndexStartupHandler> logger)
    {
        _scopeFactory = scopeFactory;
        _runtimeState = runtimeState;
        _logger = logger;
        _readiness = readiness;
    }

    public void Handle(
        UmbracoApplicationStartedNotification notification)
    {
        if (_runtimeState.Level != RuntimeLevel.Run)
        {
            return;
        }

        using Process process = Process.GetCurrentProcess();

        process.Refresh();

        long workingSetBefore = process.WorkingSet64;

        long privateMemoryBefore = process.PrivateMemorySize64;

        long managedMemoryBefore =
            GC.GetTotalMemory(
                forceFullCollection: false);

        Stopwatch semanticIndexStopwatch = Stopwatch.StartNew();

        try
        {
            using IServiceScope scope =
                _scopeFactory.CreateScope();

            IChatbotSemanticIndexBuilder indexBuilder =
                scope.ServiceProvider
                    .GetRequiredService<IChatbotSemanticIndexBuilder>();

            int candidateCount = indexBuilder.Rebuild();

            _readiness.MarkReady();

            semanticIndexStopwatch.Stop();

            process.Refresh();

            long workingSetAfter = process.WorkingSet64;

            long privateMemoryAfter = process.PrivateMemorySize64;

            long managedMemoryAfter =
                GC.GetTotalMemory(
                    forceFullCollection: false);

            double workingSetBeforeMb = ToMegabytes(workingSetBefore);

            double workingSetAfterMb = ToMegabytes(workingSetAfter);

            double privateMemoryBeforeMb = ToMegabytes(privateMemoryBefore);

            double privateMemoryAfterMb = ToMegabytes(privateMemoryAfter);

            double managedMemoryBeforeMb = ToMegabytes(managedMemoryBefore);

            double managedMemoryAfterMb = ToMegabytes(managedMemoryAfter);

            _logger.LogInformation(
                "Performance metric {MetricName} completed in {ElapsedMs} ms with {CandidateCount} candidates. " +
                "WorkingSet: {WorkingSetBeforeMb} MB -> {WorkingSetAfterMb} MB. " +
                "PrivateMemory: {PrivateMemoryBeforeMb} MB -> {PrivateMemoryAfterMb} MB. " +
                "ManagedMemory: {ManagedMemoryBeforeMb} MB -> {ManagedMemoryAfterMb} MB.",
                "SemanticIndexBuild",
                semanticIndexStopwatch.ElapsedMilliseconds,
                candidateCount,
                workingSetBeforeMb,
                workingSetAfterMb,
                privateMemoryBeforeMb,
                privateMemoryAfterMb,
                managedMemoryBeforeMb,
                managedMemoryAfterMb);
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

    private static double ToMegabytes(
    long bytes)
    {
        return Math.Round(
            bytes / 1024d / 1024d,
            2);
    }
}