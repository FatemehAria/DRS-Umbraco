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
                            <input name="WebPWD" type="WebPWD" autocomplete="current-password" required />
                        </label>

                        <button type="submit">ورود به سامانه</button>
                    </form>
                </body>
                </html>
                """);
            });

            endpoints.MapPost("/sso/login", async context =>
            {
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

                        localStorage.setItem("loginSystemName", loginData.systemName || "sama");
                        localStorage.setItem("sama-token", JSON.stringify(loginData));

                        window.location.replace("/dashboard");
                    </script>
                </body>
                </html>
                """);
            });

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
