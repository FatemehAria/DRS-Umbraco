using DrsUmbraco.Cms.Features.Chatbot.Relevance;

namespace DrsUmbraco.Cms.Tests.Features.Chatbot.Relevance;

public sealed class BgeRelevanceTokenizerTests
{
    [Fact]
    public void Encode_ShouldMatchOfficialBgeTokenizerIds()
    {
        string solutionRoot =
            FindSolutionRoot();

        string tokenizerPath =
            Path.Combine(
                solutionRoot,
                "DrsUmbraco.Cms",
                "AIModel",
                "bge-reranker-v2-m3",
                "sentencepiece.bpe.model");

        var tokenizer =
            new BgeRelevanceTokenizer(
                tokenizerPath);

        BgeRelevanceModelInput result =
            tokenizer.Encode(
                "می‌خوام پسورد حسابم رو عوض کنم.",
                "چطور رمز عبورم را تغییر بدهم؟");

        long[] expectedIds =
        [
            0,
            383,
            22914,
            376,
            5797,
            43974,
            14629,
            376,
            3085,
            117372,
            19545,
            5,
            2,
            2,
            116245,
            66888,
            63807,
            376,
            406,
            25012,
            8755,
            1692,
            1245,
            2
        ];

        Assert.Equal(
            expectedIds,
            result.InputIds);

        Assert.Equal(
            expectedIds.Length,
            result.AttentionMask.Length);

        Assert.All(
            result.AttentionMask,
            value => Assert.Equal(1L, value));
    }

    private static string FindSolutionRoot()
    {
        DirectoryInfo? directory =
            new(AppContext.BaseDirectory);

        while (directory is not null)
        {
            string solutionPath =
                Path.Combine(
                    directory.FullName,
                    "DrsUmbracoPilot.slnx");

            if (File.Exists(solutionPath))
            {
                return directory.FullName;
            }

            directory =
                directory.Parent;
        }

        throw new DirectoryNotFoundException(
            "Solution root could not be found.");
    }
}