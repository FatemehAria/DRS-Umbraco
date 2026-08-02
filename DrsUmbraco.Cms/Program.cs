using DrsUmbraco.Cms.Services;
using System.IO.Compression;
using Microsoft.AspNetCore.ResponseCompression;
using DrsUmbraco.Cms.Extensions;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// builder.Services.AddControllers();
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<IElementorSubmissionService, ElementorSubmissionService>();

builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});

builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Fastest;
});

builder.Services.Configure<GzipCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Fastest;
});

builder.Services.AddHttpClient<
    ICrmAuthenticationService,
    CrmAuthenticationService>(client =>
{
    var crmBaseUrl =
        builder.Configuration["Crm:InternalBaseUrl"];

    if (!Uri.TryCreate(
            crmBaseUrl,
            UriKind.Absolute,
            out var crmBaseUri))
    {
        throw new InvalidOperationException(
            "Crm:InternalBaseUrl is missing or invalid.");
    }

    client.BaseAddress = crmBaseUri;
    client.Timeout = TimeSpan.FromSeconds(30);
});

// builder.Services.AddHttpClient("CrmClient", client =>
// {
//     var crmBaseUrl = builder.Configuration["Crm:InternalBaseUrl"];

//     if (string.IsNullOrWhiteSpace(crmBaseUrl))
//     {
//         throw new InvalidOperationException("Crm:InternalBaseUrl is not configured.");
//     }

//     client.BaseAddress = new Uri(crmBaseUrl);
// });

builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.CreateUmbracoBuilder()
    .AddBackOffice()
    .AddWebsite()
    .AddComposers()
    .Build();

WebApplication app = builder.Build();


await app.BootUmbracoAsync();

app.UseResponseCompression();

app.UseStaticFiles();

app.MapCrmGateway();

app.UseUmbraco()
    .WithMiddleware(u =>
    {
        u.UseBackOffice();
        u.UseWebsite();
    })
    .WithEndpoints(u =>
    {
        u.EndpointRouteBuilder.MapControllers();

        u.UseBackOfficeEndpoints();
        u.UseWebsiteEndpoints();
    });

// app.MapControllers();

await app.RunAsync();
