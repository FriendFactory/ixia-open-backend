using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Common.Infrastructure.Utils;
using Frever.Cache.Resetting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Frever.Cache.Strategies;

public class DoubleCacheInMemoryBlob<TData>(Type[] dependencies, SerializeAs? serializer, bool cloneNonCachedValue)
{
    private static readonly ConcurrentDictionary<string, TData> CacheContainer = new();

    private readonly Guid _cacheGuid = Guid.NewGuid();
    private readonly bool _cloneNonCachedValue = cloneNonCachedValue;
    private readonly Type[] _dependencies = dependencies ?? throw new ArgumentNullException(nameof(dependencies));
    private readonly SerializeAs? _serializer = serializer;

    public class Cache(
        IConnectionMultiplexer redis,
        DoubleCacheInMemoryBlob<TData> settings,
        CacheDependencyTracker cacheDependencyTracker,
        ILogger<Cache> logger
    ) : IBlobCache<TData>
    {
        private readonly ILogger<Cache> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IConnectionMultiplexer _redis = redis ?? throw new ArgumentNullException(nameof(redis));
        private readonly DoubleCacheInMemoryBlob<TData> _settings = settings ?? throw new ArgumentNullException(nameof(settings));

        private readonly CacheDependencyTracker _cacheDependencyTracker =
            cacheDependencyTracker ?? throw new ArgumentNullException(nameof(cacheDependencyTracker));

        public async Task<TData> GetOrCache(string key, Func<Task<TData>> getData, TimeSpan expiration)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(key));

            var db = _redis.GetDatabase();

            var instanceMemoryBufferCacheKey = $"{key}/{_settings._cacheGuid}";

            var isCacheReset = await db.StringGetAsync(instanceMemoryBufferCacheKey) == RedisValue.Null;
            var isCached = CacheContainer.TryGetValue(key, out var cachedValue);

            if (isCacheReset || !isCached)
            {
                var data = await TryLoadFromRedisBuffer(key, getData, expiration);
                if (_settings._cloneNonCachedValue)
                    data = data.ToRedisValue(_settings._serializer).FromValue<TData>(_settings._serializer);

                _ = db.StringSetAsync(instanceMemoryBufferCacheKey, true.ToRedisValue(_settings._serializer), expiration.Spread());
                await _cacheDependencyTracker.Track(instanceMemoryBufferCacheKey, _settings._dependencies);

                CacheContainer[key] = data;

                return data;
            }

            return cachedValue;
        }

        public async Task<TData> TryGet(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(key));

            var cacheKey = $"{key}/{_settings._cacheGuid}";
            var db = _redis.GetDatabase();

            var isCached = CacheContainer.TryGetValue(key, out var cachedValue);
            var isCacheReset = await db.StringGetAsync(cacheKey) == RedisValue.Null;

            if (isCached && !isCacheReset)
                return cachedValue;

            return default;
        }

        public async Task<bool> TryModifyInPlace(string key, Func<TData, Task<TData>> getModified)
        {
            ArgumentNullException.ThrowIfNull(getModified);

            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(key));

            var cacheKey = $"{key}/{_settings._cacheGuid}";
            var db = _redis.GetDatabase();

            var isCached = CacheContainer.TryGetValue(key, out var cachedValue);
            var isCacheReset = await db.StringGetAsync(cacheKey) == RedisValue.Null;

            if (isCached && !isCacheReset)
            {
                var newValue = await getModified(cachedValue);
                CacheContainer.AddOrUpdate(key, newValue, (_, __) => newValue);
                return true;
            }

            return false;
        }

        private async Task<TData> TryLoadFromRedisBuffer(string cacheKey, Func<Task<TData>> getData, TimeSpan expiration)
        {
            ArgumentNullException.ThrowIfNull(getData);

            if (string.IsNullOrWhiteSpace(cacheKey))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(cacheKey));
            var bufferKey = $"{cacheKey}::data";
            var bufferLockKey = $"{bufferKey}::lock";
            var db = _redis.GetDatabase();
            var existingBuffer = db.StringGet(bufferKey);
            for (var i = 0; i <= 300; i++)
            {
                if (existingBuffer != RedisValue.Null)
                {
                    break;
                }

                if (db.LockTake(bufferLockKey, true, TimeSpan.FromSeconds(60)))
                {
                    try
                    {
                        var buffer = db.StringGet(bufferKey);
                        if (buffer == RedisValue.Null)
                        {
                            var data = await getData();
                            if (data == null)
                                return default;

                            db.StringSet(bufferKey, data.ToRedisValue(_settings._serializer), expiration.Spread());
                            await _cacheDependencyTracker.Track(bufferKey, _settings._dependencies);

                            return data;
                        }

                        try
                        {
                            return buffer.FromValue<TData>(_settings._serializer);
                        }
                        catch (Exception e)
                        {
                            _logger.LogWarning(e, "Failed to deserialize buffer in the loop, try again...");
                            await Task.Delay(TimeSpan.FromMilliseconds(50));
                            buffer = db.StringGet(bufferKey);
                            return buffer.FromValue<TData>(_settings._serializer);
                        }
                    }
                    finally
                    {
                        db.LockRelease(bufferLockKey, true);
                    }
                }

                await Task.Delay(TimeSpan.FromMilliseconds(200));
                existingBuffer = db.StringGet(bufferKey);
            }

            try
            {
                return existingBuffer.FromValue<TData>(_settings._serializer);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to deserialize existingBuffer, try again...");
                await Task.Delay(TimeSpan.FromMilliseconds(50));
                existingBuffer = db.StringGet(bufferKey);
                return existingBuffer.FromValue<TData>(_settings._serializer);
            }
        }
    }
}