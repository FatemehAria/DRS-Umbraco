using DrsUmbraco.Cms.Extensions;
using DrsUmbraco.Cms.Features.Chatbot.Embeddings;
using DrsUmbraco.Cms.Features.Chatbot.Services;
using DrsUmbraco.Cms.Features.Chatbot.Text;
using DrsUmbraco.Cms.Services;
using DrsUmbraco.Cms.Features.Chatbot.Notifications;
using Umbraco.Cms.Core.Notifications;

WebApplicationBuilder builder =
    WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddScoped<
    IElementorSubmissionService,
    ElementorSubmissionService>();

builder.Services.AddScoped<
    IChatbotKnowledgeService,
    UmbracoChatbotKnowledgeService>();

builder.Services.AddSingleton<
    IPersianTextNormalizer,
    PersianTextNormalizer>();

builder.Services.AddScoped<
    IChatbotMatchingService,
    ExactChatbotMatchingService>();

builder.Services.AddSingleton<LocalEmbeddingModel>();

builder.Services.AddSingleton<ILocalEmbeddingTokenizer, XlmRobertaEmbeddingTokenizer>();

builder.Services.AddSingleton<IEmbeddingService, LocalE5EmbeddingService>();

builder.Services.AddSingleton<IChatbotSemanticIndex, ChatbotSemanticIndex>();

builder.Services.AddScoped<IChatbotSemanticIndexBuilder, ChatbotSemanticIndexBuilder>();

builder.Services.AddSingleton<IChatbotSemanticSearchService, ChatbotSemanticSearchService>();

builder.Services.AddSingleton<IChatbotSemanticDecisionService, ChatbotSemanticDecisionService>();

builder.Services.AddSingleton<IChatbotSemanticCandidateFactory, ChatbotSemanticCandidateFactory>();

builder.Services.AddScoped<IChatbotSemanticIndexUpdater, ChatbotSemanticIndexUpdater>();

builder.Services.AddApplicationCompression();

builder.Services.AddCrmIntegration(
    builder.Configuration);

builder.CreateUmbracoBuilder()
    .AddBackOffice()
    .AddWebsite()
    .AddComposers()
    .AddNotificationHandler<ContentCacheRefresherNotification, ChatbotSemanticIndexCacheHandler>()
    .Build();

WebApplication app =
    builder.Build();

await app.BootUmbracoAsync();

app.UseResponseCompression();

app.UseStaticFiles();

app.MapCrmGateway();

app.UseUmbraco()
    .WithMiddleware(umbraco =>
    {
        umbraco.UseBackOffice();
        umbraco.UseWebsite();
    })
    .WithEndpoints(umbraco =>
    {
        umbraco.EndpointRouteBuilder
            .MapControllers();

        umbraco.UseBackOfficeEndpoints();
        umbraco.UseWebsiteEndpoints();
    });

await app.RunAsync();