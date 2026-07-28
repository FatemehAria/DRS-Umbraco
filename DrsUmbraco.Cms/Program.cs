using DrsUmbraco.Cms.Services;
using System.IO.Compression;
using Microsoft.AspNetCore.ResponseCompression;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
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

builder.Services.AddHttpClient("CrmClient", client =>
{
    var crmBaseUrl = builder.Configuration["Crm:InternalBaseUrl"];

    if (string.IsNullOrWhiteSpace(crmBaseUrl))
    {
        throw new InvalidOperationException("Crm:InternalBaseUrl is not configured.");
    }

    client.BaseAddress = new Uri(crmBaseUrl);
});

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

app.MapWhen(
    context => string.Equals(
        context.Request.Host.Host,
        "crm.localhost",
        StringComparison.OrdinalIgnoreCase),
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

            endpoints.MapGet("/sso/login", async context =>
        {
            PreventBrowserCache(context);

            if (HasCrmSession(context))
            {
                context.Response.Redirect("/dashboard");
                return;
            }

            var configuration = context.RequestServices.GetRequiredService<IConfiguration>();

            var mainSiteUrl =
                configuration["Site:PublicBaseUrl"]
                ?? "https://localhost:44398/";

            context.Response.ContentType = "text/html; charset=utf-8";

            await context.Response.WriteAsync($$"""
                <!doctype html>
                <html lang="fa" dir="rtl">
                <head>
                    <meta charset="utf-8" />
                    <meta name="viewport" content="width=device-width, initial-scale=1" />
                    <title>ورود به سامانه</title>

                    <link rel="stylesheet" href="/css/sama.css" />
                    <link rel="stylesheet" href="/css/pages/login-portal.css" />
                </head>
                <body class="login-portal-page">
                    <header class="login-header">
                        <a class="login-header__logo" href="{{mainSiteUrl}}" aria-label="صفحه اصلی">
                            <img src="/assets/sama/uploads/2024/11/لوگو_ی_شرکت-removebg-preview.png" alt="دیدگاه رایانه سما" />
                        </a>
                    </header>

                    <main class="login-shell">
                        <section class="login-art" aria-hidden="true">
                            <img class="login-art__image"
                                src="/assets/sama/login/login-illustration.png"
                                alt="" />
                        </section>

                        <section class="login-card" aria-labelledby="loginTitle">
                            <h1 id="loginTitle">ورود به سامانه</h1>
                            <p class="login-card__intro">
                                برای ادامه، نام کاربری و رمز عبور خود را وارد کنید.
                            </p>

                            <form class="login-form" method="post" action="/sso/login" autocomplete="on">
                                <div class="login-error" id="loginError">
                                    نام کاربری یا رمز عبور صحیح نیست.
                                </div>

                                <div class="login-field">
                                    <label for="userNo">نام کاربری</label>

                                    <div class="login-input">
                                        <input id="userNo"
                                            name="UserNo"
                                            type="text"
                                            autocomplete="username"
                                            placeholder="نام کاربری خود را وارد کنید"
                                            required />

                                        <svg class="login-input__icon" viewBox="0 0 24 24" aria-hidden="true">
                                            <path d="M12 12.2a4.2 4.2 0 1 0 0-8.4 4.2 4.2 0 0 0 0 8.4Zm0 2.1c-4.1 0-7.4 2.1-7.4 4.7 0 .7.5 1.2 1.2 1.2h12.4c.7 0 1.2-.5 1.2-1.2 0-2.6-3.3-4.7-7.4-4.7Z" fill="currentColor"/>
                                        </svg>
                                    </div>
                                </div>

                                <div class="login-field">
                                    <label for="password">رمز عبور</label>

                                    <div class="login-input">
                                        <input id="password"
                                            name="WebPWD"
                                            type="password"
                                            autocomplete="current-password"
                                            placeholder="رمز عبور خود را وارد کنید"
                                            required />

                                        <svg class="login-input__icon" viewBox="0 0 24 24" aria-hidden="true">
                                            <path d="M17 9h-1V7a4 4 0 0 0-8 0v2H7a2 2 0 0 0-2 2v7a2 2 0 0 0 2 2h10a2 2 0 0 0 2-2v-7a2 2 0 0 0-2-2Zm-7-2a2 2 0 0 1 4 0v2h-4V7Zm3 8.7V17h-2v-1.3a2 2 0 1 1 2 0Z" fill="currentColor"/>
                                        </svg>

                                        <button class="login-input__toggle"
                                                type="button"
                                                aria-label="نمایش یا مخفی کردن رمز عبور"
                                                data-password-toggle>
                                            <svg viewBox="0 0 24 24" width="22" height="22" aria-hidden="true">
                                                <path d="M12 5c5.3 0 8.7 5.1 9.4 6.3.2.4.2.9 0 1.4C20.7 13.9 17.3 19 12 19s-8.7-5.1-9.4-6.3a1.4 1.4 0 0 1 0-1.4C3.3 10.1 6.7 5 12 5Zm0 2c-3.8 0-6.5 3.4-7.4 5 .9 1.6 3.6 5 7.4 5s6.5-3.4 7.4-5c-.9-1.6-3.6-5-7.4-5Zm0 2.2a2.8 2.8 0 1 1 0 5.6 2.8 2.8 0 0 1 0-5.6Z" fill="currentColor"/>
                                            </svg>
                                        </button>
                                    </div>
                                </div>

                                <div class="login-actions">
                                    <a class="login-forgot" href="#">
                                        رمز عبور را فراموش کرده‌ام
                                    </a>
                                </div>

                                <input type="hidden" name="SystemName" value="sama" />

                                <button class="login-button" type="submit">
                                    ورود
                                </button>
                            </form>
                        </section>
                    </main>

                    <script>
                        document.querySelector("[data-password-toggle]")?.addEventListener("click", function () {
                            const input = document.getElementById("password");

                            if (!input) {
                                return;
                            }

                            input.type = input.type === "password" ? "text" : "password";
                        });
                    </script>
                </body>
                </html>
            """);
        });

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

app.UseUmbraco()
    .WithMiddleware(u =>
    {
        u.UseBackOffice();
        u.UseWebsite();
    })
    .WithEndpoints(u =>
    {
        u.UseBackOfficeEndpoints();
        u.UseWebsiteEndpoints();
    });

app.MapControllers();

await app.RunAsync();

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