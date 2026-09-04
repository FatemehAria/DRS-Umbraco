using DrsUmbraco.Cms.Features.Chatbot.Models;
using DrsUmbraco.Cms.Features.Chatbot.Relevance;
using MsOptions = Microsoft.Extensions.Options.Options;

namespace DrsUmbraco.Cms.Tests.Features.Chatbot.Relevance;

public sealed class BgeChatbotRelevanceVerifierTests
{
    [Fact]
    public void IsRelevant_WhenScoreIsAboveThreshold_ShouldReturnTrue()
    {
        var scorer =
            new FakeBgeRelevanceScorer(
                -2.5f);

        var options =
            MsOptions.Create(
                new BgeRelevanceOptions
                {
                    ModelPath = "model.onnx",
                    TokenizerPath = "tokenizer.model",
                    Threshold = -2.777814f
                });

        Lazy<IBgeRelevanceScorer> lazyScorer =
            new(() => scorer);

        var verifier =
            new BgeChatbotRelevanceVerifier(
                lazyScorer,
                options);

        ChatbotKnowledgeItem candidate =
            CreateCandidate();

        bool result =
            verifier.IsRelevant(
                "می‌خوام ایمیلم رو عوض کنم",
                candidate);

        Assert.True(result);
    }

    [Fact]
    public void IsRelevant_WhenScoreIsBelowThreshold_ShouldReturnFalse()
    {
        var scorer =
            new FakeBgeRelevanceScorer(
                -5f);

        var options =
            MsOptions.Create(
                new BgeRelevanceOptions
                {
                    ModelPath = "model.onnx",
                    TokenizerPath = "tokenizer.model",
                    Threshold = -2.777814f
                });

        Lazy<IBgeRelevanceScorer> lazyScorer =
            new(() => scorer);

        var verifier =
            new BgeChatbotRelevanceVerifier(
                lazyScorer,
                options);

        ChatbotKnowledgeItem candidate =
            CreateCandidate();

        bool result =
            verifier.IsRelevant(
                "چه مرورگرهایی پشتیبانی می‌شوند؟",
                candidate);

        Assert.False(result);
    }

    [Fact]
    public void IsRelevant_ShouldSendFullCandidateTextToScorer()
    {
        var scorer =
            new FakeBgeRelevanceScorer(
                1f);

        var options =
            MsOptions.Create(
                new BgeRelevanceOptions
                {
                    ModelPath = "model.onnx",
                    TokenizerPath = "tokenizer.model",
                    Threshold = -2.777814f
                });

        Lazy<IBgeRelevanceScorer> lazyScorer =
            new(() => scorer);

        var verifier =
            new BgeChatbotRelevanceVerifier(
                lazyScorer,
                options);

        ChatbotKnowledgeItem candidate =
            CreateCandidate();

        verifier.IsRelevant(
            "می‌خوام ایمیلم رو عوض کنم",
            candidate);

        string expected =
            string.Join(
                "\n",
                "چطور ایمیل حساب کاربری را تغییر بدهم؟",
                "تغییر ایمیل از کجاست؟",
                "می‌خواهم ایمیلم را عوض کنم",
                "برای تغییر ایمیل وارد تنظیمات حساب شوید.");

        Assert.Equal(
            expected,
            scorer.LastCandidateText);
    }

    private static ChatbotKnowledgeItem
        CreateCandidate()
    {
        return new ChatbotKnowledgeItem
        {
            Id = Guid.NewGuid(),

            Question =
                "چطور ایمیل حساب کاربری را تغییر بدهم؟",

            Answer =
                "برای تغییر ایمیل وارد تنظیمات حساب شوید.",

            AlternativeQuestions =
            [
                "تغییر ایمیل از کجاست؟",
                "می‌خواهم ایمیلم را عوض کنم"
            ],

            Kind =
                ChatbotKnowledgeItemKind.Answer
        };
    }

    private sealed class FakeBgeRelevanceScorer
        : IBgeRelevanceScorer
    {
        private readonly float _score;

        public FakeBgeRelevanceScorer(
            float score)
        {
            _score = score;
        }

        public string? LastCandidateText
        {
            get;
            private set;
        }

        public float Score(
            string question,
            string candidateText)
        {
            LastCandidateText =
                candidateText;

            return _score;
        }
    }
}