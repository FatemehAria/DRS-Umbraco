using DrsUmbraco.Cms.Features.Chatbot.Embeddings;
using DrsUmbraco.Cms.Features.Chatbot.Search;
using DrsUmbraco.Cms.Features.Chatbot.Text;

namespace DrsUmbraco.Cms.Tests.Features.Chatbot.Search;

public sealed class LexicalCorpusStatisticsBuilderTests
{
    [Fact]
    public void Build_ShouldGiveRareNGramHigherWeightThanCommonNGram()
    {
        PersianTextNormalizer normalizer =
            new();

        PersianCharacterNGramExtractor extractor =
            new(normalizer);

        LexicalCorpusStatisticsBuilder builder =
            new(extractor);

        IReadOnlyList<ChatbotSemanticCandidate> candidates =
        [
            CreateCandidate("abc xyz"),
            CreateCandidate("abc def"),
            CreateCandidate("abc ghi")
        ];

        LexicalCorpusStatistics statistics =
            builder.Build(candidates);

        float commonWeight =
            statistics.IdfWeights["abc"];

        float rareWeight =
            statistics.IdfWeights["xyz"];

        Assert.True(
            rareWeight > commonWeight);
    }

    [Fact]
    public void Build_WhenNGramAppearsInEveryDocument_ShouldGiveWeightOne()
    {
        PersianTextNormalizer normalizer =
            new();

        PersianCharacterNGramExtractor extractor =
            new(normalizer);

        LexicalCorpusStatisticsBuilder builder =
            new(extractor);

        IReadOnlyList<ChatbotSemanticCandidate> candidates =
        [
            CreateCandidate("abc xyz"),
            CreateCandidate("abc def"),
            CreateCandidate("abc ghi")
        ];

        LexicalCorpusStatistics statistics =
            builder.Build(candidates);

        Assert.Equal(
            1f,
            statistics.IdfWeights["abc"],
            precision: 5);
    }

    [Fact]
    public void Build_WhenCorpusIsEmpty_ShouldReturnEmptyWeights()
    {
        PersianTextNormalizer normalizer =
            new();

        PersianCharacterNGramExtractor extractor =
            new(normalizer);

        LexicalCorpusStatisticsBuilder builder =
            new(extractor);

        LexicalCorpusStatistics statistics =
            builder.Build([]);

        Assert.Empty(
            statistics.IdfWeights);
    }

    private static ChatbotSemanticCandidate CreateCandidate(
        string text)
    {
        return new ChatbotSemanticCandidate
        {
            KnowledgeItemId = Guid.NewGuid(),
            Text = text,
            Answer = "Test answer",
            Embedding = [1f, 0f],
            Kind = ChatbotKnowledgeItemKind.Clarification
        };
    }
}