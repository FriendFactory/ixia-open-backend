using System;
using System.Threading.Tasks;
using Common.Infrastructure.Caching.CacheKeys;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Frever.Cache.Resetting;

/// <summary>
///     Tracks dependencies of cache keys from certain entities.
/// </summary>
public class CacheDependencyTracker
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger _redisCacheDeleteLogger;

    public CacheDependencyTracker(IConnectionMultiplexer redis, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);

        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
        _redisCacheDeleteLogger = loggerFactory.CreateLogger("Frever.Redis");
    }

    public async Task Track(string cacheKey, Type[] dependencies)
    {
        ArgumentNullException.ThrowIfNull(dependencies);

        if (string.IsNullOrWhiteSpace(cacheKey))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(cacheKey));

        var db = _redis.GetDatabase();

        foreach (var dep in dependencies)
        {
            var depKey = DependencyKey(dep);
            await db.SetAddAsync(depKey, cacheKey);
        }
    }

    public async Task TrackUser(long groupId, string cacheKey, Type[] dependencies)
    {
        ArgumentNullException.ThrowIfNull(dependencies);

        if (string.IsNullOrWhiteSpace(cacheKey))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(cacheKey));

        var db = _redis.GetDatabase();

        foreach (var dep in dependencies)
        {
            var depKey = DependencyUserKey(dep, groupId);
            await db.SetAddAsync(depKey, cacheKey);
        }
    }

    public Task Reset(Type dependency, long? groupId = null)
    {
        ArgumentNullException.ThrowIfNull(dependency);

        var db = _redis.GetDatabase();
        {
            var depKey = DependencyKey(dependency);

            foreach (var val in db.SetScan(depKey))
            {
                _redisCacheDeleteLogger.LogInformation("CacheDependencyTracker.Reset: deleting cache key {0}", (string) val);
                db.KeyDelete((string) val);
            }

            _redisCacheDeleteLogger.LogInformation("CacheDependencyTracker.Reset: deleting cache key {0}", depKey);
            db.KeyDelete(depKey);
        }

        if (groupId != null)
        {
            var depUserKey = DependencyUserKey(dependency, groupId.Value);

            foreach (var val in db.SetScan(depUserKey))
            {
                _redisCacheDeleteLogger.LogInformation("CacheDependencyTracker.Reset: deleting cache key {0}", (string) val);
                db.KeyDelete((string) val);
            }

            _redisCacheDeleteLogger.LogInformation("CacheDependencyTracker.Reset: deleting cache key {0}", depUserKey);
            db.KeyDelete(depUserKey);
        }

        return Task.CompletedTask;
    }

    private static string DependencyKey(Type dependency)
    {
        return $"dependency::{dependency.FullName}".FreverAssetCacheKey().GetKeyWithoutVersion();
    }

    private static string DependencyUserKey(Type dependency, long groupId)
    {
        return $"dependency::{dependency.FullName}".FreverAssetCacheKey().GetKeyWithoutVersion().CachePerUser(groupId);
    }
}