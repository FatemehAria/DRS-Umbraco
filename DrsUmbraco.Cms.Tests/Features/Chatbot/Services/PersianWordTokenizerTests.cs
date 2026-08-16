using DrsUmbraco.Cms.Features.Chatbot.Services;
using DrsUmbraco.Cms.Features.Chatbot.Text;

namespace DrsUmbraco.Cms.Tests.Features.Chatbot.Services;

public sealed class PersianWordTokenizerTests
{
    [Fact]
    public void Tokenize_WhenTextHasWords_ShouldReturnWords()
    {
        PersianWordTokenizer tokenizer =
            new(new PersianTextNormalizer());

        IReadOnlyList<string> result =
            tokenizer.Tokenize(
                "حسابم قفل شده است");

        Assert.Equal(
            ["حسابم", "قفل", "شده", "است"],
            result);
    }

    [Fact]
    public void Tokenize_WhenTextContainsPunctuation_ShouldRemovePunctuation()
    {
        PersianWordTokenizer tokenizer =
            new(new PersianTextNormalizer());

        IReadOnlyList<string> result =
            tokenizer.Tokenize(
                "حسابم قفل شده، چیکار کنم؟");

        Assert.Equal(
            ["حسابم", "قفل", "شده", "چیکار", "کنم"],
            result);
    }

    [Fact]
    public void Tokenize_WhenTextContainsArabicCharacters_ShouldNormalizeThem()
    {
        PersianWordTokenizer tokenizer =
            new(new PersianTextNormalizer());

        IReadOnlyList<string> result =
            tokenizer.Tokenize(
                "تغيير كلمه");

        Assert.Equal(
            ["تغییر", "کلمه"],
            result);
    }

    [Fact]
    public void Tokenize_WhenTextIsEmpty_ShouldReturnEmptyList()
    {
        PersianWordTokenizer tokenizer =
            new(new PersianTextNormalizer());

        IReadOnlyList<string> result =
            tokenizer.Tokenize("   ");

        Assert.Empty(result);
    }
}