using DrsUmbraco.Cms.Features.Chatbot.Search;

namespace DrsUmbraco.Cms.Tests.Features.Chatbot.Search;

public sealed class WeightedLexicalSimilarityCalculatorTests
{
    [Fact]
    public void Calculate_WhenRareFeatureMatches_ShouldScoreHigherThanCommonFeature()
    {
        FakeCharacterNGramExtractor extractor =
            new(new Dictionary<string, IReadOnlySet<string>>
            {
                ["common-query"] =
                    new HashSet<string> { "common", "q" },

                ["common-candidate"] =
                    new HashSet<string> { "common", "c" },

                ["rare-query"] =
                    new HashSet<string> { "rare", "q" },

                ["rare-candidate"] =
                    new HashSet<string> { "rare", "c" }
            });

        WeightedLexicalSimilarityCalculator calculator =
            new(extractor);

        LexicalCorpusStatistics statistics =
            new()
            {
                IdfWeights =
                    new Dictionary<string, float>
                    {
                        ["common"] = 1f,
                        ["rare"] = 3f,
                        ["q"] = 1f,
                        ["c"] = 1f
                    },

                UnseenIdfWeight = 4f
            };

        float commonScore =
            calculator.Calculate(
                "common-query",
                "common-candidate",
                statistics);

        float rareScore =
            calculator.Calculate(
                "rare-query",
                "rare-candidate",
                statistics);

        Assert.True(
            rareScore > commonScore);
    }

    [Fact]
    public void Calculate_WhenFeatureSetsAreIdentical_ShouldReturnOne()
    {
        FakeCharacterNGramExtractor extractor =
            new(new Dictionary<string, IReadOnlySet<string>>
            {
                ["first"] =
                    new HashSet<string>
                    {
                        "abc",
                        "xyz"
                    },

                ["second"] =
                    new HashSet<string>
                    {
                        "abc",
                        "xyz"
                    }
            });

        WeightedLexicalSimilarityCalculator calculator =
            new(extractor);

        LexicalCorpusStatistics statistics =
            new()
            {
                IdfWeights =
                    new Dictionary<string, float>
                    {
                        ["abc"] = 1f,
                        ["xyz"] = 3f
                    },

                UnseenIdfWeight = 4f
            };

        float score =
            calculator.Calculate(
                "first",
                "second",
                statistics);

        Assert.Equal(
            1f,
            score,
            precision: 5);
    }

    [Fact]
    public void Calculate_WhenThereIsNoOverlap_ShouldReturnZero()
    {
        FakeCharacterNGramExtractor extractor =
            new(new Dictionary<string, IReadOnlySet<string>>
            {
                ["first"] =
                    new HashSet<string>
                    {
                        "abc"
                    },

                ["second"] =
                    new HashSet<string>
                    {
                        "xyz"
                    }
            });

        WeightedLexicalSimilarityCalculator calculator =
            new(extractor);

        LexicalCorpusStatistics statistics =
            new()
            {
                IdfWeights =
                    new Dictionary<string, float>
                    {
                        ["abc"] = 1f,
                        ["xyz"] = 3f
                    },

                UnseenIdfWeight = 4f
            };

        float score =
            calculator.Calculate(
                "first",
                "second",
                statistics);

        Assert.Equal(
            0f,
            score);
    }

    private sealed class FakeCharacterNGramExtractor
        : ICharacterNGramExtractor
    {
        private readonly IReadOnlyDictionary<
            string,
            IReadOnlySet<string>> _values;

        public FakeCharacterNGramExtractor(
            IReadOnlyDictionary<
                string,
                IReadOnlySet<string>> values)
        {
            _values = values;
        }

        public IReadOnlySet<string> Extract(
            string? text)
        {
            if (text is null)
            {
                return new HashSet<string>();
            }

            return _values.TryGetValue(
                text,
                out IReadOnlySet<string>? value)
                    ? value
                    : new HashSet<string>();
        }
    }
}