using DrsUmbraco.Cms.Extensions;
using DrsUmbraco.Cms.Features.Chatbot.Embeddings;
using DrsUmbraco.Cms.Features.Chatbot.Services;
using DrsUmbraco.Cms.Features.Chatbot.Text;
using DrsUmbraco.Cms.Services;
using DrsUmbraco.Cms.Features.Chatbot.Notifications;
using Umbraco.Cms.Core.Notifications;
using DrsUmbraco.Cms.Features.Chatbot.Configuration;
using DrsUmbraco.Cms.Features.Chatbot.Search;

WebApplicationBuilder builder =
    WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddScoped<IElementorSubmissionService, ElementorSubmissionService>();

builder.Services.AddScoped<IChatbotKnowledgeService, UmbracoChatbotKnowledgeService>();

builder.Services.AddSingleton<IPersianTextNormalizer, PersianTextNormalizer>();

builder.Services.AddScoped<IChatbotMatchingService, ExactChatbotMatchingService>();

builder.Services.AddSingleton<LocalEmbeddingModel>();

builder.Services.AddSingleton<ILocalEmbeddingTokenizer, XlmRobertaEmbeddingTokenizer>();

builder.Services.AddSingleton<IEmbeddingService, LocalE5EmbeddingService>();

builder.Services.AddSingleton<IChatbotSemanticIndex, ChatbotSemanticIndex>();

builder.Services.AddScoped<IChatbotSemanticIndexBuilder, ChatbotSemanticIndexBuilder>();

builder.Services.AddSingleton<IChatbotSemanticSearchService, ChatbotSemanticSearchService>();

builder.Services.AddSingleton<IChatbotSemanticDecisionService, ChatbotSemanticDecisionService>();

builder.Services.AddSingleton<IChatbotSemanticCandidateFactory, ChatbotSemanticCandidateFactory>();

builder.Services.AddSingleton<ILexicalSimilarityCalculator, PersianLexicalSimilarityCalculator>();

builder.Services.AddScoped<IChatbotSemanticIndexUpdater, ChatbotSemanticIndexUpdater>();

builder.Services.AddSingleton<IChatbotHybridSearchService, ChatbotHybridSearchService>();

builder.Services.AddSingleton<ICharacterNGramExtractor, PersianCharacterNGramExtractor>();

builder.Services.AddSingleton<ILexicalSimilarityCalculator, PersianLexicalSimilarityCalculator>();

builder.Services.AddSingleton<ILexicalCorpusStatisticsBuilder, LexicalCorpusStatisticsBuilder>();

builder.Services.AddSingleton<IWeightedLexicalSimilarityCalculator, WeightedLexicalSimilarityCalculator>();

builder.Services.AddSingleton<IChatbotWeightedHybridSearchService, ChatbotWeightedHybridSearchService>();

builder.Services.AddScoped<IChatbotMessageService, ChatbotMessageService>();

builder.Services
    .AddOptions<ChatbotSemanticDecisionOptions>()
    .Bind(
        builder.Configuration.GetSection(
            ChatbotSemanticDecisionOptions.SectionName))
    .Validate(
        options =>
            options.MinimumScore >= 0f &&
            options.MinimumScore <= 1f,
        "Chatbot MinimumScore must be between 0 and 1.")
    .Validate(
        options =>
            options.MinimumMargin >= 0f &&
            options.MinimumMargin <= 1f,
        "Chatbot MinimumMargin must be between 0 and 1.")
    .ValidateOnStart();

builder.Services.AddApplicationCompression();

builder.Services.AddCrmIntegration(
    builder.Configuration);

builder.CreateUmbracoBuilder()
    .AddBackOffice()
    .AddWebsite()
    .AddComposers()
    .AddNotificationHandler<
        ContentCacheRefresherNotification,
        ChatbotSemanticIndexCacheHandler>()
    .AddNotificationHandler<
        UmbracoApplicationStartedNotification,
        ChatbotSemanticIndexStartupHandler>()
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