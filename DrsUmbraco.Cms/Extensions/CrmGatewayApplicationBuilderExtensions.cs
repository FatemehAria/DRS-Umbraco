namespace DrsUmbraco.Cms.Extensions;

public static class CrmGatewayApplicationBuilderExtensions
{
    public static void MapCrmGateway(this WebApplication app)
    {
        // تنظیمات و MapWhen فعلی اینجا منتقل می‌شود.

        var crmGatewayHosts =
    app.Configuration
        .GetSection("Crm:GatewayHosts")
        .Get<string[]>()
    ?? Array.Empty<string>();

        var crmGatewayPorts =
            app.Configuration
                .GetSection("Crm:GatewayPorts")
                .Get<int[]>()
            ?? Array.Empty<int>();

        app.MapWhen(
            context =>
            {
                var hostMatches = crmGatewayHosts.Contains(
                    context.Request.Host.Host,
                    StringComparer.OrdinalIgnoreCase);

                var portMatches =
                    context.Request.Host.Port is int port &&
                    crmGatewayPorts.Contains(port);

                var path = context.Request.Path;

                var isLocalAuthenticationRoute =
                    path.StartsWithSegments("/sso/login") ||
                    path.StartsWithSegments("/sso/logout") ||
                    path.StartsWithSegments("/login");

                if (isLocalAuthenticationRoute)
                {
                    return false;
                }

                return hostMatches || portMatches;
            },
            proxyApp =>
            {
                proxyApp.UseRouting();

                proxyApp.UseStaticFiles();

                proxyApp.Use(async (context, next) =>
                {
                    var path = context.Request.Path;

                    var isStaticAssetPath =
                        path.StartsWithSegments("/css") ||
                        path.StartsWithSegments("/js") ||
                        path.StartsWithSegments("/assets") ||
                        path.StartsWithSegments("/media") ||
                        path.StartsWithSegments("/umbraco");

                    if (isStaticAssetPath)
                    {
                        await next();
                        return;
                    }

                    var hasGatewaySession =
                        context.Request.Cookies.ContainsKey("CrmGatewaySession");

                    if (!hasGatewaySession)
                    {
                        if (path.StartsWithSegments("/api"))
                        {
                            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                            return;
                        }

                        context.Response.Redirect("/sso/login?expired=1");
                        return;
                    }

                    await next();
                });

                proxyApp.UseEndpoints(endpoints =>
                {
                    endpoints.MapReverseProxy();
                });
            });

    }

}