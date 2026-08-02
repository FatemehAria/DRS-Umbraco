using DrsUmbraco.Cms.Extensions;
using DrsUmbraco.Cms.Services;

WebApplicationBuilder builder =
    WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddScoped<
    IElementorSubmissionService,
    ElementorSubmissionService>();

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

/*
 * تنظیم فعلی Static Files خودت را بدون تغییر نگه دار.
 * مخصوصاً اگر WebP mapping یا محافظ asset اضافه کرده‌ای.
 */
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