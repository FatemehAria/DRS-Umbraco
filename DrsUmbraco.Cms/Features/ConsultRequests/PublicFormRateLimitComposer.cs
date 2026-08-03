using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Web.Common.ApplicationBuilder;

namespace DrsUmbraco.Cms.Features.ConsultRequests;

public static class RateLimitPolicyNames
{
    public const string PublicFormByIp = "public-form-by-ip";
}

public sealed class PublicFormRateLimitComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode =
                StatusCodes.Status429TooManyRequests;

            options.AddPolicy<string>(
                RateLimitPolicyNames.PublicFormByIp,
                httpContext =>
                {
                    var clientIp =
                        httpContext.Connection.RemoteIpAddress?.ToString()
                        ?? "unknown";

                    return RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: clientIp,
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            // هر IP حداکثر ۵ درخواست
                            PermitLimit = 3,

                            // در هر بازه ۱۰ دقیقه‌ای
                            // Window = TimeSpan.FromMinutes(10),
                            Window = TimeSpan.FromSeconds(30),

                            // درخواست اضافی منتظر نماند؛ فوراً رد شود
                            QueueLimit = 0,

                            QueueProcessingOrder =
                                QueueProcessingOrder.OldestFirst,

                            AutoReplenishment = true
                        });
                });

            options.OnRejected = async (context, cancellationToken) =>
            {
                var retryAfter = TimeSpan.FromMinutes(10);

                if (context.Lease.TryGetMetadata(
                        MetadataName.RetryAfter,
                        out var actualRetryAfter))
                {
                    retryAfter = actualRetryAfter;
                }

                var retryAfterSeconds =
                    Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds));

                context.HttpContext.Response.Headers["Retry-After"] =
                    retryAfterSeconds.ToString(CultureInfo.InvariantCulture);

                await context.HttpContext.Response.WriteAsJsonAsync(
                    new
                    {
                        title = "تعداد درخواست‌ها بیش از حد مجاز است.",
                        status = StatusCodes.Status429TooManyRequests,
                        detail =
                            "لطفاً چند دقیقه صبر کنید و سپس دوباره تلاش کنید."
                    },
                    cancellationToken: cancellationToken);
            };
        });

        builder.Services.Configure<UmbracoPipelineOptions>(options =>
        {
            options.AddFilter(
                new UmbracoPipelineFilter("PublicFormRateLimiting")
                {
                    PostRouting = postRouting =>
                    {
                        postRouting.UseRateLimiter();
                    }
                });
        });
    }
}