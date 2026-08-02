using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.StaticFiles;

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

                var isLoginPageRequest =
                    HttpMethods.IsGet(context.Request.Method) &&
                    string.Equals(
                        context.Request.Path.Value,
                        "/sso/login",
                        StringComparison.OrdinalIgnoreCase);

                if (isLoginPageRequest)
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

                    var isSsoPath =
                        path.StartsWithSegments("/sso/login") ||
                        path.StartsWithSegments("/sso/logout") ||
                        path.StartsWithSegments("/login");

                    var isStaticAssetPath =
                        path.StartsWithSegments("/css") ||
                        path.StartsWithSegments("/js") ||
                        path.StartsWithSegments("/assets") ||
                        path.StartsWithSegments("/media") ||
                        path.StartsWithSegments("/umbraco");

                    if (isSsoPath || isStaticAssetPath)
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

                    endpoints.MapPost("/sso/login", async context =>
                    {
                        PreventBrowserCache(context);
                        var form = await context.Request.ReadFormAsync();

                        var UserNo = form["UserNo"].ToString();
                        var WebPWD = form["WebPWD"].ToString();

                        if (string.IsNullOrWhiteSpace(UserNo) || string.IsNullOrWhiteSpace(WebPWD))
                        {
                            context.Response.StatusCode = StatusCodes.Status400BadRequest;
                            await context.Response.WriteAsync("Username and password are required.");
                            return;
                        }

                        var httpClientFactory = context.RequestServices.GetRequiredService<IHttpClientFactory>();
                        var configuration = context.RequestServices.GetRequiredService<IConfiguration>();

                        var loginPath = configuration["Crm:LoginPath"] ?? "/api/users/login";

                        var crmClient = httpClientFactory.CreateClient("CrmClient");

                        var loginPayload = new
                        {
                            UserNo,
                            WebPWD,
                            SystemName = "sama",
                        };

                        var crmResponse = await crmClient.PostAsJsonAsync(loginPath, loginPayload);

                        var responseBody = await crmResponse.Content.ReadAsStringAsync();

                        if (!crmResponse.IsSuccessStatusCode)
                        {
                            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                            context.Response.ContentType = "text/plain; charset=utf-8";
                            await context.Response.WriteAsync("Login failed.");
                            return;
                        }

                        foreach (var setCookieHeader in crmResponse.Headers.TryGetValues("Set-Cookie", out var cookies)
                            ? cookies
                            : Enumerable.Empty<string>())
                        {
                            context.Response.Headers.Append("Set-Cookie", setCookieHeader);
                        }

                        var gatewaySessionExpires = GetCrmGatewaySessionExpires(responseBody, configuration);
                        var gatewaySessionMaxAge = gatewaySessionExpires - DateTimeOffset.UtcNow;

                        if (gatewaySessionMaxAge <= TimeSpan.Zero)
                        {
                            gatewaySessionExpires = DateTimeOffset.UtcNow.AddMinutes(30);
                            gatewaySessionMaxAge = TimeSpan.FromMinutes(30);
                        }

                        context.Response.Cookies.Append(
                            "CrmGatewaySession",
                            "1",
                            new CookieOptions
                            {
                                HttpOnly = true,
                                Secure = true,
                                SameSite = SameSiteMode.Lax,
                                Expires = gatewaySessionExpires,
                                MaxAge = gatewaySessionMaxAge
                            });

                        context.Response.ContentType = "text/html; charset=utf-8";

                        var safeJson = System.Text.Json.JsonSerializer.Serialize(responseBody);

                        await context.Response.WriteAsync($$"""
                <!doctype html>
                <html lang="fa" dir="rtl">
                <head>
                    <meta charset="utf-8" />
                    <title>در حال ورود...</title>
                </head>
                <body>
                    <p>در حال ورود به سامانه...</p>

                    <script>
                        const responseText = {{safeJson}};
                        const loginData = JSON.parse(responseText);

                        try {
                            const configItems = JSON.parse(loginData.config || "[]");

                            for (const item of configItems) {
                                const name = (item.ParamName || "").toLowerCase();

                                if (name === "apiaddress" || name === "baseaddress") {
                                    item.ParamValue = "/api/";
                                }

                                if (name === "filesimulate") {
                                    item.ParamValue = "/api/misc/filesimulate";
                                }
                            }

                            loginData.config = JSON.stringify(configItems);
                        } catch (error) {
                            console.error(error);
                        }

                        localStorage.setItem("loginSystemName", loginData.systemName || "sama");
                        localStorage.setItem("sama-token", JSON.stringify(loginData));

                        window.location.replace("/dashboard");
                    </script>
                </body>
                </html>
                """);
                    });

                    endpoints.MapGet("/sso/logout", HandleCrmLogoutRedirect);
                    endpoints.MapGet("/sso/logout/{**catchAll}", HandleCrmLogoutRedirect);

                    endpoints.MapGet("/login", HandleCrmLogoutRedirect);
                    endpoints.MapGet("/login/{**catchAll}", HandleCrmLogoutRedirect);

                    endpoints.MapReverseProxy();
                });
            });

    }

    static void ExpireCrmCookie(HttpContext context, string name)
    {
        context.Response.Headers.Append(
            "Set-Cookie",
            $"{name}=; path=/; expires=Thu, 01 Jan 1970 00:00:00 GMT; max-age=0; secure; samesite=lax");
    }

    static void PreventBrowserCache(HttpContext context)
    {
        context.Response.Headers.CacheControl = "no-store, no-cache, max-age=0, must-revalidate";
        context.Response.Headers.Pragma = "no-cache";
        context.Response.Headers.Expires = "0";
    }

    static bool HasCrmSession(HttpContext context)
    {
        return context.Request.Cookies.ContainsKey("CrmGatewaySession");
    }

    static void ExpireAllCrmCookies(HttpContext context)
    {
        ExpireCrmCookie(context, ".SAMA.Session");
        ExpireCrmCookie(context, "SystemName");
        ExpireCrmCookie(context, "X-Token");
        ExpireCrmCookie(context, "X-Ip");
        ExpireCrmCookie(context, "CrmGatewaySession");
    }

    static DateTimeOffset GetCrmGatewaySessionExpires(
        string responseBody,
        IConfiguration configuration)
    {
        var fallbackExpires = DateTimeOffset.UtcNow.AddMinutes(30);

        try
        {
            using var document = System.Text.Json.JsonDocument.Parse(responseBody);

            var expireText = GetJsonStringCaseInsensitive(
                document.RootElement,
                "expireDateTime");

            if (string.IsNullOrWhiteSpace(expireText))
            {
                return fallbackExpires;
            }

            if (!DateTime.TryParse(
                    expireText,
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None,
                    out var crmLocalDateTime))
            {
                return fallbackExpires;
            }

            var crmTimeZone = GetCrmTimeZone(configuration);

            var unspecifiedCrmDateTime = DateTime.SpecifyKind(
                crmLocalDateTime,
                DateTimeKind.Unspecified);

            var utcDateTime = TimeZoneInfo.ConvertTimeToUtc(
                unspecifiedCrmDateTime,
                crmTimeZone);

            var expireDateTime = new DateTimeOffset(utcDateTime, TimeSpan.Zero);

            if (expireDateTime <= DateTimeOffset.UtcNow)
            {
                return fallbackExpires;
            }

            return expireDateTime;
        }
        catch
        {
            return fallbackExpires;
        }
    }

    static string? GetJsonStringCaseInsensitive(
        System.Text.Json.JsonElement element,
        string propertyName)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(
                    property.Name,
                    propertyName,
                    StringComparison.OrdinalIgnoreCase))
            {
                return property.Value.GetString();
            }
        }

        return null;
    }

    static TimeZoneInfo GetCrmTimeZone(IConfiguration configuration)
    {
        var configuredTimeZoneId = configuration["Crm:TimeZoneId"];

        if (!string.IsNullOrWhiteSpace(configuredTimeZoneId))
        {
            return TimeZoneInfo.FindSystemTimeZoneById(configuredTimeZoneId);
        }

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Iran Standard Time");
        }
        catch
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Asia/Tehran");
        }
    }

    static async Task HandleCrmLogoutRedirect(HttpContext context)
    {
        PreventBrowserCache(context);
        var configuration = context.RequestServices.GetRequiredService<IConfiguration>();

        var logoutRedirectUrl =
            configuration["Crm:LogoutRedirectUrl"]
            ?? "https://localhost:44398/customer-portal/";

        ExpireAllCrmCookies(context);

        context.Response.ContentType = "text/html; charset=utf-8";

        await context.Response.WriteAsync($$"""
    <!doctype html>
    <html lang="fa" dir="rtl">
    <head>
        <meta charset="utf-8" />
        <title>خروج از سامانه</title>
    </head>
    <body>
        <p>در حال انتقال به پرتال مشتریان...</p>

        <script>
            try {
                localStorage.removeItem("loginSystemName");
                localStorage.removeItem("sama-token");
                sessionStorage.clear();
            } catch (error) {
                console.error(error);
            }

            window.location.replace("{{logoutRedirectUrl}}");
        </script>
    </body>
    </html>
    """);
    }

}