using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Common.Infrastructure.Utils;
using Frever.Cache.Resetting;
using StackExchange.Redis;

namespace Frever.Cache.Strategies;

public class InMemoryBlob<TData>(Type[] dependencies, SerializeAs? serializer, bool cloneNonCachedValue)
{
    private static readonly ConcurrentDictionary<string, TData> CacheContainer = new();

    private readonly Guid _cacheGuid = Guid.NewGuid();
    private readonly bool _cloneNonCachedValue = cloneNonCachedValue;
    private readonly Type[] _dependencies = dependencies ?? throw new ArgumentNullException(nameof(dependencies));
    private readonly SerializeAs? _serializer = serializer;

    public class Cache(IConnectionMultiplexer redis, InMemoryBlob<TData> settings, CacheDependencyTracker cacheDependencyTracker)
        : IBlobCache<TData>
    {
        private readonly CacheDependencyTracker _cacheDependencyTracker = cacheDependencyTracker ?? throw new ArgumentNullException(nameof(cacheDependencyTracker));
        private readonly IConnectionMultiplexer _redis = redis ?? throw new ArgumentNullException(nameof(redis));
        private readonly InMemoryBlob<TData> _settings = settings ?? throw new ArgumentNullException(nameof(settings));

        public async Task<TData> GetOrCache(string key, Func<Task<TData>> getData, TimeSpan expiration)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(key));

            var db = _redis.GetDatabase();

            var cacheKey = $"{key}/{_settings._cacheGuid}";
            var isCacheReset = await db.StringGetAsync(cacheKey) == RedisValue.Null;
            var isCached = CacheContainer.TryGetValue(key, out var cachedValue);

            if (isCacheReset || !isCached)
            {
                var data = await getData();
                if (_settings._cloneNonCachedValue)
                    data = data.ToRedisValue().FromValue<TData>();

                _ = db.StringSetAsync(cacheKey, true.ToRedisValue(), expiration.Spread());
                await _cacheDependencyTracker.Track(cacheKey, _settings._dependencies);

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
                CacheContainer.AddOrUpdate(key, newValue, (_, _) => newValue);
                return true;
            }

            return false;
        }
    }
}