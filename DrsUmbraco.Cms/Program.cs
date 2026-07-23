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

app.MapWhen(
    context => string.Equals(
        context.Request.Host.Host,
        "crm.localhost",
        StringComparison.OrdinalIgnoreCase),
    proxyApp =>
    {
        proxyApp.UseRouting();

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
                context.Response.ContentType = "text/html; charset=utf-8";

                await context.Response.WriteAsync("""
                <!doctype html>
                <html lang="fa" dir="rtl">
                <head>
                    <meta charset="utf-8" />
                    <meta name="viewport" content="width=device-width, initial-scale=1" />
                    <title>ورود به سامانه پشتیبانی</title>
                    <style>
                        body {
                            margin: 0;
                            min-height: 100vh;
                            display: grid;
                            place-items: center;
                            font-family: Tahoma, Arial, sans-serif;
                            background: #f3f7fb;
                            color: #0b3558;
                        }

                        form {
                            width: min(420px, calc(100% - 32px));
                            padding: 32px;
                            border-radius: 24px;
                            background: white;
                            box-shadow: 0 24px 70px rgba(8, 38, 66, .12);
                        }

                        h1 {
                            margin: 0 0 20px;
                            font-size: 1.5rem;
                        }

                        label {
                            display: block;
                            margin-top: 16px;
                            font-weight: 700;
                        }

                        input {
                            width: 100%;
                            box-sizing: border-box;
                            margin-top: 8px;
                            padding: 12px 14px;
                            border: 1px solid #d9e4ee;
                            border-radius: 14px;
                            font: inherit;
                            direction: ltr;
                        }

                        button {
                            width: 100%;
                            margin-top: 24px;
                            padding: 13px 16px;
                            border: 0;
                            border-radius: 999px;
                            background: #0b4c7d;
                            color: white;
                            font: inherit;
                            font-weight: 800;
                            cursor: pointer;
                        }
                    </style>
                </head>
                <body>
                    <form method="post" action="/sso/login">
                        <h1>ورود به سامانه پشتیبانی</h1>

                        <label>
                            نام کاربری
                            <input name="UserNo" autocomplete="UserNo" required />
                        </label>

                        <label>
                            رمز عبور
                            <input name="WebPWD" type="password" autocomplete="current-password" required />
                        </label>

                        <button type="submit">ورود به سامانه</button>
                    </form>
                </body>

                <script>
                    try {
                        const raw = localStorage.getItem("sama-token");

                        if (raw) {
                            const loginData = JSON.parse(raw);
                            const expireDate = loginData.expireDateTime
                                ? new Date(loginData.expireDateTime)
                                : null;

                            if (!expireDate || expireDate > new Date()) {
                                window.location.replace("/dashboard");
                            } else {
                                localStorage.removeItem("loginSystemName");
                                localStorage.removeItem("sama-token");
                                sessionStorage.clear();
                            }
                        }
                    } catch (error) {
                        localStorage.removeItem("loginSystemName");
                        localStorage.removeItem("sama-token");
                        sessionStorage.clear();
                    }
                </script>
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
    return context.Request.Cookies.ContainsKey("X-Token")
        || context.Request.Cookies.ContainsKey("SystemName");
}


static async Task HandleCrmLogoutRedirect(HttpContext context)
{
    PreventBrowserCache(context);
    var configuration = context.RequestServices.GetRequiredService<IConfiguration>();

    var logoutRedirectUrl =
        configuration["Crm:LogoutRedirectUrl"]
        ?? "https://localhost:44398/customer-portal/";

    ExpireCrmCookie(context, ".SAMA.Session");
    ExpireCrmCookie(context, "SystemName");
    ExpireCrmCookie(context, "X-Token");
    ExpireCrmCookie(context, "X-Ip");

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