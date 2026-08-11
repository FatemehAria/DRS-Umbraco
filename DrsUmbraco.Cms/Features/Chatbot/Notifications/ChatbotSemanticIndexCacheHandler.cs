using DrsUmbraco.Cms.Features.Chatbot.Embeddings;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.Changes;

namespace DrsUmbraco.Cms.Features.Chatbot.Notifications;

public sealed class ChatbotSemanticIndexCacheHandler
    : INotificationHandler<ContentCacheRefresherNotification>
{
    private const string FaqContentTypeAlias = "chatbotFaqItem";
    private const string KnowledgeBaseContentTypeAlias = "chatbotKnowledgeBase";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IChatbotSemanticIndex _semanticIndex;
    private readonly ILogger<ChatbotSemanticIndexCacheHandler> _logger;

    public ChatbotSemanticIndexCacheHandler(
        IServiceScopeFactory scopeFactory,
        IChatbotSemanticIndex semanticIndex,
        ILogger<ChatbotSemanticIndexCacheHandler> logger)
    {
        _scopeFactory = scopeFactory;
        _semanticIndex = semanticIndex;
        _logger = logger;
    }

    public void Handle(
        ContentCacheRefresherNotification notification)
    {
        if (notification.MessageObject
            is not ContentCacheRefresher.JsonPayload[] payloads)
        {
            return;
        }

        using IServiceScope scope = _scopeFactory.CreateScope();

        IContentService contentService =
            scope.ServiceProvider
                .GetRequiredService<IContentService>();

        IChatbotSemanticIndexUpdater updater =
            scope.ServiceProvider
                .GetRequiredService<IChatbotSemanticIndexUpdater>();

        IChatbotSemanticIndexBuilder builder =
            scope.ServiceProvider
                .GetRequiredService<IChatbotSemanticIndexBuilder>();

        foreach (ContentCacheRefresher.JsonPayload payload in payloads)
        {
            bool isRelevantChange =
                (payload.ChangeTypes &
                    (TreeChangeTypes.RefreshNode |
                     TreeChangeTypes.RefreshBranch |
                     TreeChangeTypes.Remove))
                != 0;

            if (!isRelevantChange)
            {
                continue;
            }

            var content =
                contentService.GetById(payload.Id);

            if (content is not null)
            {
                if (content.ContentType.Alias ==
                    FaqContentTypeAlias)
                {
                    updater.Refresh(content.Key);

                    _logger.LogInformation(
                        "Chatbot semantic index refreshed for FAQ {Key}.",
                        content.Key);

                    continue;
                }

                if (content.ContentType.Alias ==
                    KnowledgeBaseContentTypeAlias)
                {
                    builder.Rebuild();

                    _logger.LogInformation(
                        "Chatbot semantic index was fully rebuilt.");

                    continue;
                }

                continue;
            }

            // Content may have been deleted.
            if (payload.Key is Guid key)
            {
                bool existsInSemanticIndex =
                    _semanticIndex
                        .GetAll()
                        .Any(candidate =>
                            candidate.KnowledgeItemId == key);

                if (existsInSemanticIndex)
                {
                    _semanticIndex.RemoveForKnowledgeItem(key);

                    _logger.LogInformation(
                        "FAQ {Key} was removed from chatbot semantic index.",
                        key);
                }
            }
        }
    }
}