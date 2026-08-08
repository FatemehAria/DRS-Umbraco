using DrsUmbraco.Cms.Features.Chatbot.Text;

namespace DrsUmbraco.Cms.Tests.Features.Chatbot.Text;

public sealed class PersianTextNormalizerTests
{
    [Fact]
    public void Normalize_WhenTextContainsArabicCharacters_ShouldConvertThemToPersian()
    {
        // Arrange
        PersianTextNormalizer normalizer = new();
        string input = "كاربر";

        // Act
        string result = normalizer.Normalize(input);

        // Assert
        Assert.Equal("کاربر", result);
    }

    [Fact]
    public void Normalize_WhenTextIsNull_ShouldReturnEmptyString()
    {
        // Arrange
        PersianTextNormalizer normalizer = new();
        string? input = null;

        // Act
        string result = normalizer.Normalize(input);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Normalize_WhenTextContainsPunctuation_ShouldReplaceIt()
    {
        // Arrange
        PersianTextNormalizer normalizer = new();
        string input = "سلام!";

        // Act
        string result = normalizer.Normalize(input);

        // Assert
        Assert.Equal("سلام", result);
    }

    [Fact]
    public void Normalize_WhenTextContainsDiacritics_ShouldRemoveThem()
    {
        // Arrange
        PersianTextNormalizer normalizer = new();
        string input = "صِدا";

        // Act
        string result = normalizer.Normalize(input);

        // Assert
        Assert.Equal("صدا", result);
    }

    [Fact]
    public void Normalize_WhenTextContainsExtraSpaces_ShouldCollapseThem()
    {
        // Arrange
        PersianTextNormalizer normalizer = new();
        string input = "  سلام    ";

        // Act
        string result = normalizer.Normalize(input);

        // Assert
        Assert.Equal("سلام", result);
    }

    [Fact]
    public void Normalize_WhenTextContainsZwnj_ShouldReplaceItWithSpace()
    {
        // Arrange
        PersianTextNormalizer normalizer = new();
        string input = "می‌روم";

        // Act
        string result = normalizer.Normalize(input);

        // Assert
        Assert.Equal("می روم", result);
    }

    [Fact]
    public void Normalize_WhenTextContainsUppercaseEnglish_ShouldConvertItToLowercase()
    {
        // Arrange
        PersianTextNormalizer normalizer = new();
        string input = "LOGIN Error";

        // Act
        string result = normalizer.Normalize(input);

        // Assert
        Assert.Equal("login error", result);
    }
}