using System.Text;

namespace DrsUmbraco.Cms.Middleware;

public sealed class CrmGatewayHtmlInjectionMiddleware
{
    private const string BridgeScriptPath =
        "/js/crm-gateway-bridge.js";

    private const string BridgeScriptTag =
        "<script src=\"/js/crm-gateway-bridge.js\"></script>";

    private readonly RequestDelegate _next;

    public CrmGatewayHtmlInjectionMiddleware(
        RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context)
    {
        var acceptsHtml =
            context.Request.Headers.Accept.Any(
                value =>
                    value?.Contains(
                        "text/html",
                        StringComparison.OrdinalIgnoreCase)
                    == true);

        if (!acceptsHtml)
        {
            await _next(context);
            return;
        }

        // اجازه نمی‌دهیم CRM پاسخ HTML فشرده برگرداند،
        // چون می‌خواهیم HTML را قبل از ارسال به Browser تغییر دهیم.
        context.Request.Headers.Remove(
            "Accept-Encoding");

        var originalResponseBody =
            context.Response.Body;

        await using var buffer =
            new MemoryStream();

        context.Response.Body = buffer;

        try
        {
            await _next(context);

            buffer.Position = 0;

            var contentType =
                context.Response.ContentType;

            var isHtml =
                !string.IsNullOrWhiteSpace(contentType) &&
                contentType.StartsWith(
                    "text/html",
                    StringComparison.OrdinalIgnoreCase);

            if (!isHtml)
            {
                await buffer.CopyToAsync(
                    originalResponseBody);

                return;
            }

            using var reader =
                new StreamReader(
                    buffer,
                    Encoding.UTF8,
                    detectEncodingFromByteOrderMarks: true,
                    bufferSize: 1024,
                    leaveOpen: true);

            var html =
                await reader.ReadToEndAsync();

            if (!html.Contains(
                    BridgeScriptPath,
                    StringComparison.OrdinalIgnoreCase))
            {
                var bodyClosingIndex =
                    html.LastIndexOf(
                        "</body>",
                        StringComparison.OrdinalIgnoreCase);

                if (bodyClosingIndex >= 0)
                {
                    html = html.Insert(
                        bodyClosingIndex,
                        BridgeScriptTag);
                }
                else
                {
                    html += BridgeScriptTag;
                }
            }

            // چون body را تغییر داده‌ایم،
            // metadata قبلی دیگر معتبر نیست.
            context.Response.Headers.Remove(
                "Content-Length");

            context.Response.Headers.Remove(
                "ETag");

            var bytes =
                Encoding.UTF8.GetBytes(html);

            context.Response.ContentLength =
                bytes.Length;

            await originalResponseBody.WriteAsync(
                bytes);
        }
        finally
        {
            context.Response.Body =
                originalResponseBody;
        }
    }
}