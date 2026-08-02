using DrsUmbraco.Cms.Options;
using DrsUmbraco.Cms.Services;
using Microsoft.Extensions.Options;

namespace DrsUmbraco.Cms.Extensions;

public static class CrmServiceCollectionExtensions
{
    public static IServiceCollection AddCrmIntegration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddCrmOptions(configuration);
        services.AddSiteOptions(configuration);

        services.AddScoped<
            ICrmSessionService,
            CrmSessionService>();

        services.AddHttpClient<
            ICrmAuthenticationService,
            CrmAuthenticationService>(
            (serviceProvider, client) =>
            {
                var crmOptions =
                    serviceProvider
                        .GetRequiredService<
                            IOptions<CrmOptions>>()
                        .Value;

                client.BaseAddress =
                    new Uri(
                        crmOptions.InternalBaseUrl,
                        UriKind.Absolute);

                client.Timeout =
                    TimeSpan.FromSeconds(30);
            });

        services
            .AddReverseProxy()
            .LoadFromConfig(
                configuration.GetSection(
                    "ReverseProxy"));

        return services;
    }

    private static void AddCrmOptions(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<CrmOptions>()
            .Bind(
                configuration.GetSection(
                    CrmOptions.SectionName))
            .Validate(
                options =>
                    Uri.TryCreate(
                        options.InternalBaseUrl,
                        UriKind.Absolute,
                        out var uri) &&
                    IsHttpUri(uri),
                "Crm:InternalBaseUrl must be a valid absolute HTTP or HTTPS URL.")
            .Validate(
                options =>
                    !string.IsNullOrWhiteSpace(
                        options.LoginPath) &&
                    options.LoginPath.StartsWith('/'),
                "Crm:LoginPath must start with '/'.")
            .Validate(
                options =>
                    options.GatewayHosts.Any(
                        host =>
                            !string.IsNullOrWhiteSpace(
                                host)) ||
                    options.GatewayPorts.Length > 0,
                "At least one CRM gateway host or port must be configured.")
            .Validate(
                options =>
                    options.GatewayPorts.All(
                        port =>
                            port is >= 1 and <= 65535),
                "Every CRM gateway port must be between 1 and 65535.")
            .Validate(
                options =>
                    Uri.TryCreate(
                        options.LogoutRedirectUrl,
                        UriKind.Absolute,
                        out var uri) &&
                    IsHttpUri(uri),
                "Crm:LogoutRedirectUrl must be a valid absolute HTTP or HTTPS URL.")
            .ValidateOnStart();
    }

    private static void AddSiteOptions(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<SiteOptions>()
            .Bind(
                configuration.GetSection(
                    SiteOptions.SectionName))
            .Validate(
                options =>
                    Uri.TryCreate(
                        options.PublicBaseUrl,
                        UriKind.Absolute,
                        out var uri) &&
                    IsHttpUri(uri),
                "Site:PublicBaseUrl must be a valid absolute HTTP or HTTPS URL.")
            .ValidateOnStart();
    }

    private static bool IsHttpUri(Uri uri)
    {
        return uri.Scheme == Uri.UriSchemeHttp ||
               uri.Scheme == Uri.UriSchemeHttps;
    }
}