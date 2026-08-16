using DrsUmbraco.Cms.Features.Chatbot.Services;

namespace DrsUmbraco.Cms.Tests.Features.Chatbot.Services;

public sealed class PersianBm25TermFilterTests
{
    [Fact]
    public void Filter_ShouldRemoveStopWords()
    {
        PersianBm25TermFilter filter =
            new();

        IReadOnlyList<string> result =
            filter.Filter(
                [
                    "اسم",
                    "و",
                    "اطلاعات",
                    "از",
                    "کجا",
                    "اصلاح",
                    "کنم"
                ]);

        Assert.Equal(
            [
                "اسم",
                "اطلاعات",
                "اصلاح"
            ],
            result);
    }

    [Fact]
    public void Filter_ShouldKeepImportantIntentTerms()
    {
        PersianBm25TermFilter filter =
            new();

        IReadOnlyList<string> result =
            filter.Filter(
                [
                    "قفل",
                    "ورود",
                    "رمز",
                    "پروفایل"
                ]);

        Assert.Equal(
            [
                "قفل",
                "ورود",
                "رمز",
                "پروفایل"
            ],
            result);
    }
}