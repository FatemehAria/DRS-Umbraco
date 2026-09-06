using DrsUmbraco.Cms.Features.Chatbot.Models;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Cryptography;
using System.Text;

namespace DrsUmbraco.Cms.Features.Chatbot.Caching;

public sealed class ChatbotResponseMemoryCache
    : IChatbotResponseCache,
      IDisposable
{
    private const long MaximumEntryCount = 500;

    private static readonly TimeSpan SlidingExpiration = TimeSpan.FromMinutes(15);

    private static readonly TimeSpan AbsoluteExpiration = TimeSpan.FromHours(1);

    private readonly MemoryCache _cache;

    private long _version;

    public ChatbotResponseMemoryCache()
    {
        _cache =
            new MemoryCache(
                new MemoryCacheOptions
                {
                    SizeLimit =
                        MaximumEntryCount
                });
    }

    public long CaptureVersion()
    {
        return Volatile.Read(ref _version);
    }

    public bool TryGet(
        string normalizedQuestion,
        long version,
        out ChatbotMessageResult? result)
    {
        ValidateNormalizedQuestion(normalizedQuestion);

        CacheKey key = CreateCacheKey(version, normalizedQuestion);

        return _cache.TryGetValue(
            key,
            out result);
    }

    public void Set(
        string normalizedQuestion,
        long version,
        ChatbotMessageResult result)
    {
        ValidateNormalizedQuestion(
            normalizedQuestion);

        ArgumentNullException.ThrowIfNull(
            result);

        // اگر هنگام محاسبه پاسخ، Cache منقضی شده باشد،
        // نتیجه متعلق به نسخه قدیمی را ذخیره نمی‌کنیم.
        if (version != CaptureVersion())
        {
            return;
        }

        CacheKey key = CreateCacheKey(version, normalizedQuestion);

        _cache.Set(
            key,
            result,
            new MemoryCacheEntryOptions
            {
                Size = 1,
                SlidingExpiration =
                    SlidingExpiration,
                AbsoluteExpirationRelativeToNow =
                    AbsoluteExpiration
            });
    }

    public void Invalidate()
    {
        Interlocked.Increment(ref _version);

        _cache.Compact(percentage: 1.0);
    }

    public void Dispose()
    {
        _cache.Dispose();
    }

    private static void ValidateNormalizedQuestion(
        string normalizedQuestion)
    {
        if (string.IsNullOrWhiteSpace(
                normalizedQuestion))
        {
            throw new ArgumentException(
                "Normalized question is required.",
                nameof(normalizedQuestion));
        }
    }

    private readonly record struct CacheKey(long Version, string QuestionHash);

    private static CacheKey CreateCacheKey(
        long version,
        string normalizedQuestion)
    {
        return new CacheKey(
            version,
            CreateQuestionHash(
                normalizedQuestion));
    }

    private static string CreateQuestionHash(
        string normalizedQuestion)
    {
        byte[] questionBytes =
            Encoding.UTF8.GetBytes(
                normalizedQuestion);

        byte[] hashBytes =
            SHA256.HashData(
                questionBytes);

        return Convert.ToHexString(
            hashBytes);
    }
}