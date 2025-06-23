using System;
using System.Globalization;
using Common.Infrastructure.Caching.CacheKeys;

namespace Frever.Cache;

public static class CacheKeys
{
    public static readonly string FreverPrefix = "ixia".FreverVersionedCache();

    public static readonly string FreverAssetPrefix = $"{FreverPrefix}::assets";

    public static string FreverCacheKey(this string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(key));

        return $"{FreverPrefix}::{key}";
    }

    public static string FreverCacheKeyWithoutVersion(this string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(key));

        return $"{FreverPrefix}::{key}".GetKeyWithoutVersion();
    }

    public static string FreverAssetCacheKey(this string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(key));

        return $"{FreverAssetPrefix}::{key}";
    }

    public static string CachePerUser(this string cacheKey, long groupId)
    {
        if (string.IsNullOrWhiteSpace(cacheKey))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(cacheKey));

        return $"{cacheKey}::{{{groupId}}}";
    }

    public static string CacheDaily(this string cacheKey, DateTime date)
    {
        if (string.IsNullOrWhiteSpace(cacheKey))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(cacheKey));

        return $"{cacheKey}::{date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}";
    }
}