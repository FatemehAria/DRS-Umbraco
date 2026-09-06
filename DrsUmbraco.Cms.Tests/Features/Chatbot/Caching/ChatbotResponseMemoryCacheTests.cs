using DrsUmbraco.Cms.Features.Chatbot.Caching;
using DrsUmbraco.Cms.Features.Chatbot.Models;

namespace DrsUmbraco.Cms.Tests.Features.Chatbot.Caching;

public sealed class ChatbotResponseMemoryCacheTests
{
    [Fact]
    public void Set_WhenEntryIsValid_ShouldMakeResultAvailable()
    {
        using var cache = new ChatbotResponseMemoryCache();

        const string normalizedQuestion = "چطور رمز عبورم را تغییر بدهم";

        long version = cache.CaptureVersion();

        ChatbotMessageResult expectedResult = CreateFallbackResult();

        cache.Set(
            normalizedQuestion,
            version,
            expectedResult);

        bool wasFound =
            cache.TryGet(
                normalizedQuestion,
                version,
                out ChatbotMessageResult? actualResult);

        Assert.True(wasFound);

        Assert.Same(
            expectedResult,
            actualResult);
    }

    [Fact]
    public void Invalidate_ShouldMakePreviousEntryUnavailable()
    {
        using var cache = new ChatbotResponseMemoryCache();

        const string normalizedQuestion = "چطور رمز عبورم را تغییر بدهم";

        long previousVersion = cache.CaptureVersion();

        cache.Set(
            normalizedQuestion,
            previousVersion,
            CreateFallbackResult());

        cache.Invalidate();

        long currentVersion =
            cache.CaptureVersion();

        bool wasFound =
            cache.TryGet(
                normalizedQuestion,
                currentVersion,
                out _);

        Assert.NotEqual(
            previousVersion,
            currentVersion);

        Assert.False(wasFound);
    }

    [Fact]
    public void Set_WhenVersionIsStale_ShouldNotStoreResult()
    {
        using var cache = new ChatbotResponseMemoryCache();

        const string normalizedQuestion = "چطور رمز عبورم را تغییر بدهم";

        long staleVersion = cache.CaptureVersion();

        cache.Invalidate();

        cache.Set(
            normalizedQuestion,
            staleVersion,
            CreateFallbackResult());

        long currentVersion = cache.CaptureVersion();

        bool wasFound =
            cache.TryGet(
                normalizedQuestion,
                currentVersion,
                out _);

        Assert.False(wasFound);
    }

    private static ChatbotMessageResult
        CreateFallbackResult()
    {
        return new ChatbotMessageResult
        {
            ResponseType = ChatbotResponseType.Fallback,

            Reply = "پاسخ دقیقی پیدا نشد."
        };
    }
}