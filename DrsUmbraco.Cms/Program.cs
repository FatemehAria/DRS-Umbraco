using DrsUmbraco.Cms.Extensions;
using DrsUmbraco.Cms.Features.Chatbot.Services;
using DrsUmbraco.Cms.Features.Chatbot.Text;
using DrsUmbraco.Cms.Services;

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

builder.Services.AddApplicationCompression();

builder.Services.AddCrmIntegration(
    builder.Configuration);

builder.CreateUmbracoBuilder()
    .AddBackOffice()
    .AddWebsite()
    .AddComposers()
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