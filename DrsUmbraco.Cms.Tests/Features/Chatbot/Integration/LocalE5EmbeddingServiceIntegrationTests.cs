// متن رو بدیم و embed 384 بعدی بده.
using DrsUmbraco.Cms.Features.Chatbot.Embeddings;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;

namespace DrsUmbraco.Cms.Tests.Features.Chatbot.Integration;

public sealed class LocalE5EmbeddingServiceIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    public void Generate_WithRealModel_ShouldReturn384Dimensions()
    {
        IWebHostEnvironment environment =
            CreateEnvironment();

        XlmRobertaEmbeddingTokenizer tokenizer =
            new(environment);

        using LocalEmbeddingModel model =
            new(environment);

        LocalE5EmbeddingService service =
            new(
                tokenizer,
                model);

        float[] embedding =
            service.Generate(
                "چطور رمز عبورم را تغییر بدهم؟");

        Assert.Equal(
            384,
            embedding.Length);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public void Generate_WithRealModel_ShouldReturnL2NormalizedVector()
    {
        IWebHostEnvironment environment =
            CreateEnvironment();

        XlmRobertaEmbeddingTokenizer tokenizer =
            new(environment);

        using LocalEmbeddingModel model =
            new(environment);

        LocalE5EmbeddingService service =
            new(
                tokenizer,
                model);

        float[] embedding =
            service.Generate(
                "چطور رمز عبورم را تغییر بدهم؟");

        double sumOfSquares = 0;

        foreach (float value in embedding)
        {
            sumOfSquares += value * value;
        }

        double magnitude =
            Math.Sqrt(sumOfSquares);

        Assert.InRange(
            magnitude,
            0.9999,
            1.0001);
    }

    private static IWebHostEnvironment CreateEnvironment()
    {
        string cmsProjectPath =
            Path.GetFullPath(
                Path.Combine(
                    AppContext.BaseDirectory,
                    "..",
                    "..",
                    "..",
                    "..",
                    "DrsUmbraco.Cms"));

        return new TestWebHostEnvironment
        {
            ContentRootPath = cmsProjectPath
        };
    }

    private sealed class TestWebHostEnvironment
        : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = string.Empty;

        public IFileProvider WebRootFileProvider { get; set; } =
            new NullFileProvider();

        public string WebRootPath { get; set; } = string.Empty;

        public string EnvironmentName { get; set; } =
            "Development";

        public string ContentRootPath { get; set; } =
            string.Empty;

        public IFileProvider ContentRootFileProvider { get; set; } =
            new NullFileProvider();
    }
}