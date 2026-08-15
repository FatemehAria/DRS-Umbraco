using DrsUmbraco.Cms.Features.Chatbot.Search;
using DrsUmbraco.Cms.Features.Chatbot.Text;

namespace DrsUmbraco.Cms.Tests.Features.Chatbot.Search;

public sealed class PersianLexicalSimilarityCalculatorTests
{
    private readonly PersianLexicalSimilarityCalculator _calculator =
        new(new PersianTextNormalizer());

    [Fact]
    public void Calculate_WhenTextsAreIdentical_ShouldReturnOne()
    {
        float score =
            _calculator.Calculate(
                "تغییر رمز عبور",
                "تغییر رمز عبور");

        Assert.Equal(
            1f,
            score);
    }

    [Fact]
    public void Calculate_WhenTextsAreUnrelated_ShouldReturnLowScore()
    {
        float score =
            _calculator.Calculate(
                "رمز عبور",
                "هوای امروز");

        Assert.InRange(
            score,
            0f,
            0.1f);
    }

    [Fact]
    public void Calculate_WhenWordsHaveDifferentSuffixes_ShouldDetectSimilarity()
    {
        float similarScore =
            _calculator.Calculate(
                "شماره موبایلم",
                "شماره موبایل");

        float unrelatedScore =
            _calculator.Calculate(
                "شماره موبایلم",
                "حذف حساب");

        Assert.True(
            similarScore > unrelatedScore);
    }

    [Fact]
    public void Calculate_WhenTextIsEmpty_ShouldReturnZero()
    {
        float score =
            _calculator.Calculate(
                "",
                "تغییر رمز");

        Assert.Equal(
            0f,
            score);
    }
}