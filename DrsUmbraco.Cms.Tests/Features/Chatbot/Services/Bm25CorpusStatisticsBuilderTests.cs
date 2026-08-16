using DrsUmbraco.Cms.Features.Chatbot.Services;
using DrsUmbraco.Cms.Features.Chatbot.Text;

namespace DrsUmbraco.Cms.Tests.Features.Chatbot.Services;

public sealed class Bm25CorpusStatisticsBuilderTests
{
    [Fact]
    public void Build_ShouldCalculateDocumentFrequencyAndAverageLength()
    {
        PersianWordTokenizer tokenizer =
            new(new PersianTextNormalizer());

        Bm25CorpusStatisticsBuilder builder =
            new(tokenizer);

        Bm25CorpusStatistics statistics =
            builder.Build(
                [
                    "حساب قفل قفل",
                    "حساب ورود"
                ]);

        Assert.Equal(
            2,
            statistics.DocumentCount);

        Assert.Equal(
            2.5f,
            statistics.AverageDocumentLength);

        Assert.Equal(
            2,
            statistics.DocumentFrequencies["حساب"]);

        Assert.Equal(
            1,
            statistics.DocumentFrequencies["قفل"]);

        Assert.Equal(
            1,
            statistics.DocumentFrequencies["ورود"]);
    }
}