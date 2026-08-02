using System.IO.Compression;
using Microsoft.AspNetCore.ResponseCompression;

namespace DrsUmbraco.Cms.Extensions;

public static class CompressionServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationCompression(
        this IServiceCollection services)
    {
        services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;

            options.Providers.Add<
                BrotliCompressionProvider>();

            options.Providers.Add<
                GzipCompressionProvider>();
        });

        services.Configure<
            BrotliCompressionProviderOptions>(options =>
        {
            options.Level =
                CompressionLevel.Fastest;
        });

        services.Configure<
            GzipCompressionProviderOptions>(options =>
        {
            options.Level =
                CompressionLevel.Fastest;
        });

        return services;
    }
}