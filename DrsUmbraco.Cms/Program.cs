using DrsUmbraco.Cms.Extensions;
using DrsUmbraco.Cms.Features.Chatbot.Embeddings;
using DrsUmbraco.Cms.Features.Chatbot.Services;
using DrsUmbraco.Cms.Features.Chatbot.Text;
using DrsUmbraco.Cms.Services;
using DrsUmbraco.Cms.Features.Chatbot.Notifications;
using Umbraco.Cms.Core.Notifications;
using DrsUmbraco.Cms.Features.Chatbot.Configuration;
using DrsUmbraco.Cms.Features.Chatbot.Search;
using DrsUmbraco.Cms.Features.Chatbot.Relevance;
using Microsoft.Extensions.Options;
using System.Diagnostics;

Stopwatch applicationStartupStopwatch = Stopwatch.StartNew();

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddScoped<IElementorSubmissionService, ElementorSubmissionService>();

builder.Services.AddScoped<IChatbotKnowledgeService, UmbracoChatbotKnowledgeService>();

builder.Services.AddSingleton<IPersianTextNormalizer, PersianTextNormalizer>();

builder.Services.AddScoped<IChatbotMatchingService, ExactChatbotMatchingService>();

builder.Services.AddSingleton<LocalEmbeddingModel>();

builder.Services.AddSingleton<ILocalEmbeddingTokenizer, XlmRobertaEmbeddingTokenizer>();

builder.Services.AddSingleton<EmbeddingPerformanceMetrics>();

builder.Services.AddSingleton<IEmbeddingService, LocalE5EmbeddingService>();

builder.Services.AddSingleton<IChatbotSemanticIndex, ChatbotSemanticIndex>();

builder.Services.AddScoped<IChatbotSemanticIndexBuilder, ChatbotSemanticIndexBuilder>();

builder.Services.AddSingleton<IChatbotSemanticSearchService, ChatbotSemanticSearchService>();

builder.Services.AddSingleton<IChatbotSemanticDecisionService, ChatbotSemanticDecisionService>();

builder.Services.AddSingleton<IChatbotSemanticCandidateFactory, ChatbotSemanticCandidateFactory>();

builder.Services.AddScoped<IChatbotSemanticIndexUpdater, ChatbotSemanticIndexUpdater>();

builder.Services.AddSingleton<IChatbotHybridSearchService, ChatbotHybridSearchService>();

builder.Services.AddSingleton<ICharacterNGramExtractor, PersianCharacterNGramExtractor>();

builder.Services.AddSingleton<ILexicalSimilarityCalculator, PersianLexicalSimilarityCalculator>();

builder.Services.AddSingleton<ILexicalCorpusStatisticsBuilder, LexicalCorpusStatisticsBuilder>();

builder.Services.AddSingleton<IWeightedLexicalSimilarityCalculator, WeightedLexicalSimilarityCalculator>();

builder.Services.AddSingleton<IChatbotWeightedHybridSearchService, ChatbotWeightedHybridSearchService>();

builder.Services.AddSingleton<IChatbotSemanticRankingService, ChatbotSemanticRankingService>();

builder.Services.AddSingleton<IChatbotWeightedLexicalRankingService, ChatbotWeightedLexicalRankingService>();

builder.Services.AddSingleton<IEmbeddingCentroidCalculator, EmbeddingCentroidCalculator>();

builder.Services.AddSingleton<IChatbotSemanticCentroidRankingService, ChatbotSemanticCentroidRankingService>();

builder.Services.AddSingleton<IChatbotRerankingService, ChatbotRerankingService>();

builder.Services.AddSingleton<IPersianWordTokenizer, PersianWordTokenizer>();

builder.Services.AddSingleton<IBm25CorpusStatisticsBuilder, Bm25CorpusStatisticsBuilder>();

builder.Services.AddSingleton<IBm25SimilarityCalculator, Bm25SimilarityCalculator>();

builder.Services.AddSingleton<IBm25TermFilter, PersianBm25TermFilter>();

builder.Services.AddSingleton<IChatbotBm25RankingService, ChatbotBm25RankingService>();

builder.Services.AddSingleton<IChatbotCandidateEvidenceService, ChatbotCandidateEvidenceService>();

builder.Services.AddSingleton<IChatbotAmbiguityEvidenceService, ChatbotAmbiguityEvidenceService>();

builder.Services.AddSingleton<IChatbotCandidateIntentSetBuilder, ChatbotCandidateIntentSetBuilder>();

builder.Services.AddSingleton<IChatbotCandidateIntentSetEvidenceService, ChatbotCandidateIntentSetEvidenceService>();

builder.Services.AddScoped<IChatbotClarificationExactMatchingService, ChatbotClarificationExactMatchingService>();

builder.Services.AddScoped<IChatbotDiscriminativeEvidenceService, ChatbotDiscriminativeEvidenceService>();

// builder.Services.AddSingleton<IChatbotRelevanceVerifier, AllowAllChatbotRelevanceVerifier>();

builder.Services
    .AddOptions<BgeRelevanceOptions>()
    .Bind(
        builder.Configuration.GetSection(
            BgeRelevanceOptions.SectionName))
    .Validate(
        options =>
            !string.IsNullOrWhiteSpace(
                options.ModelPath),
        "BGE relevance ModelPath is required.")
    .Validate(
        options =>
            !string.IsNullOrWhiteSpace(
                options.TokenizerPath),
        "BGE relevance TokenizerPath is required.")
    .Validate(
        options =>
            float.IsFinite(
                options.Threshold),
        "BGE relevance Threshold must be finite.")
    .ValidateOnStart();

builder.Services.AddSingleton<
    BgeRelevanceTokenizer>(
    serviceProvider =>
    {
        ILogger<BgeRelevanceTokenizer> logger =
            serviceProvider.GetRequiredService<
                ILogger<BgeRelevanceTokenizer>>();

        IWebHostEnvironment environment =
            serviceProvider
                .GetRequiredService<
                    IWebHostEnvironment>();

        BgeRelevanceOptions options =
            serviceProvider
                .GetRequiredService<
                    IOptions<BgeRelevanceOptions>>()
                .Value;

        string tokenizerPath =
            Path.IsPathRooted(
                options.TokenizerPath)
                ? options.TokenizerPath
                : Path.Combine(
                    environment.ContentRootPath,
                    options.TokenizerPath);

        return new BgeRelevanceTokenizer(
            Path.GetFullPath(tokenizerPath),
            logger);
    });

builder.Services.AddSingleton<
    BgeRelevanceModel>(
    serviceProvider =>
    {
        ILogger<BgeRelevanceModel> logger =
            serviceProvider.GetRequiredService<
                ILogger<BgeRelevanceModel>>();

        IWebHostEnvironment environment =
            serviceProvider
                .GetRequiredService<
                    IWebHostEnvironment>();

        BgeRelevanceOptions options =
            serviceProvider
                .GetRequiredService<
                    IOptions<BgeRelevanceOptions>>()
                .Value;

        string modelPath =
            Path.IsPathRooted(
                options.ModelPath)
                ? options.ModelPath
                : Path.Combine(
                    environment.ContentRootPath,
                    options.ModelPath);

        return new BgeRelevanceModel(
            Path.GetFullPath(modelPath),
            logger);
    });

builder.Services.AddSingleton<IBgeRelevanceScorer, BgeRelevanceScorer>();

builder.Services.AddSingleton<IChatbotRelevanceVerifier, BgeChatbotRelevanceVerifier>();

builder.Services
    .AddOptions<ChatbotNoMatchDecisionOptions>()
    .Bind(
        builder.Configuration.GetSection(
            ChatbotNoMatchDecisionOptions.SectionName))
    .Validate(
        options =>
            options.MinimumSemanticScore >= 0f &&
            options.MinimumSemanticScore <= 1f,
        "Chatbot MinimumSemanticScore must be between 0 and 1.")
    .ValidateOnStart();

builder.Services.AddSingleton<
    IChatbotNoMatchDecisionService,
    ChatbotNoMatchDecisionService>();

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

Stopwatch umbracoBootStopwatch =
    Stopwatch.StartNew();

await app.BootUmbracoAsync();

umbracoBootStopwatch.Stop();

app.Logger.LogInformation(
    "Performance metric {MetricName} completed in {ElapsedMs} ms.",
    "UmbracoBoot",
    umbracoBootStopwatch.ElapsedMilliseconds);

app.UseResponseCompression();

app.UseStaticFiles();

ILogger chatbotPerformanceLogger =
    app.Services
        .GetRequiredService<ILoggerFactory>()
        .CreateLogger("ChatbotPerformance");

int chatbotRequestCount = 0;

app.Use(
    async (context, next) =>
    {
        if (!context.Request.Path.StartsWithSegments(
                "/api/chatbot"))
        {
            await next();

            return;
        }

        int requestNumber =
            Interlocked.Increment(
                ref chatbotRequestCount);

        Stopwatch requestStopwatch =
            Stopwatch.StartNew();

        try
        {
            await next();
        }
        finally
        {
            requestStopwatch.Stop();

            chatbotPerformanceLogger.LogInformation(
                "Performance metric {MetricName} completed in {ElapsedMs} ms. " +
                "RequestNumber={RequestNumber}, Path={RequestPath}, StatusCode={StatusCode}.",
                "ChatbotHttpRequest",
                requestStopwatch.Elapsed.TotalMilliseconds,
                requestNumber,
                context.Request.Path.Value,
                context.Response.StatusCode);
        }
    });

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

app.Lifetime.ApplicationStarted.Register(
    () =>
    {
        applicationStartupStopwatch.Stop();

        app.Logger.LogInformation(
            "Performance metric {MetricName} completed in {ElapsedMs} ms.",
            "ApplicationStartup",
            applicationStartupStopwatch.ElapsedMilliseconds);
    });

await app.RunAsync();