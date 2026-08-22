using DrsUmbraco.Cms.Features.Chatbot.Relevance;

namespace DrsUmbraco.Cms.Tests.Features.Chatbot.Relevance;

public sealed class BgeRelevanceModelTests
{
    [Fact]
    public void Run_ShouldMatchValidatedPythonScore()
    {
        string solutionRoot =
            FindSolutionRoot();

        string modelPath =
            Path.Combine(
                solutionRoot,
                "DrsUmbraco.Cms",
                "AIModel",
                "bge-reranker-v2-m3",
                "model.onnx");

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

        BgeRelevanceModelInput input =
            tokenizer.Encode(
                "می‌خوام پسورد حسابم رو عوض کنم.",
                "چطور رمز عبورم را تغییر بدهم؟");

        using var model =
            new BgeRelevanceModel(
                modelPath);

        float score =
            model.Run(input);

        const float expectedScore =
            2.822576761f;

        Assert.InRange(
            score,
            expectedScore - 0.00001f,
            expectedScore + 0.00001f);
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