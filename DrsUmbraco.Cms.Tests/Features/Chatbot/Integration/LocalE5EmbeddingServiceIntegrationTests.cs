using DrsUmbraco.Cms.Features.Chatbot.Embeddings;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;

namespace DrsUmbraco.Cms.Tests.Features.Chatbot.Integration;

public sealed class LocalE5EmbeddingServiceIntegrationTests
{
    // متن رو بدیم و embed 384 بعدی بده.

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

    // طول بردار واقعا یک میشه؟ نرمالایز میشه؟

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

    // تست score ها

    [Fact]
    [Trait("Category", "Integration")]
    public void Generate_SimilarTexts_ShouldBeMoreSimilarThanUnrelatedTexts()
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

        float[] originalEmbedding =
            service.Generate(
                "چطور رمز عبورم را تغییر بدهم؟");

        float[] similarEmbedding =
            service.Generate(
                "پسوردمو چجوری عوض کنم؟");

        float[] unrelatedEmbedding =
            service.Generate(
                "امروز هوا چطوره؟");

        float similarScore =
            CosineSimilarityCalculator.Calculate(
                originalEmbedding,
                similarEmbedding);

        float unrelatedScore =
            CosineSimilarityCalculator.Calculate(
                originalEmbedding,
                unrelatedEmbedding);

        Assert.True(
            similarScore > unrelatedScore,
            $"Expected similar score ({similarScore}) " +
            $"to be greater than unrelated score ({unrelatedScore}).");
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