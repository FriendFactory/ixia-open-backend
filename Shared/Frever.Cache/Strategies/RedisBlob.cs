using System;
using System.Threading.Tasks;
using Common.Infrastructure.Utils;
using Frever.Cache.Resetting;
using StackExchange.Redis;

namespace Frever.Cache.Strategies;

public class RedisBlob<TData>
{
    private readonly Type[] _dependencies;
    private readonly SerializeAs? _serializer;

    public RedisBlob(Type[] dependencies, SerializeAs? serializer, bool cloneNonCachedValue)
    {
        _dependencies = dependencies ?? throw new ArgumentNullException(nameof(dependencies));
        _serializer = serializer;
    }

    public class Cache(IConnectionMultiplexer redis, RedisBlob<TData> settings, CacheDependencyTracker cacheDependencyTracker)
        : IBlobCache<TData>
    {
        private readonly IConnectionMultiplexer _redis = redis ?? throw new ArgumentNullException(nameof(redis));
        private readonly RedisBlob<TData> _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        private readonly CacheDependencyTracker _cacheDependencyTracker = cacheDependencyTracker ?? throw new ArgumentNullException(nameof(cacheDependencyTracker));

        public async Task<TData> GetOrCache(string key, Func<Task<TData>> getData, TimeSpan expiration)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(key));

            var db = _redis.GetDatabase();

            var cachedValue = await db.StringGetAsync(key);

            if (cachedValue == RedisValue.Null)
            {
                var data = await getData();

                _ = db.StringSetAsync(key, data.ToRedisValue(_settings._serializer), expiration.Spread());
                await _cacheDependencyTracker.Track(key, _settings._dependencies);

                return data;
            }

            return cachedValue.FromValue<TData>(_settings._serializer);
        }

        public async Task<TData> TryGet(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(key));

            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(key));

            var db = _redis.GetDatabase();

            var cachedValue = await db.StringGetAsync(key);
            if (cachedValue.HasValue)
                return cachedValue.FromValue<TData>(_settings._serializer);

            return default;
        }

        public async Task<bool> TryModifyInPlace(string key, Func<TData, Task<TData>> getModified)
        {
            ArgumentNullException.ThrowIfNull(getModified);

            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(key));

            var db = _redis.GetDatabase();

            var cachedValue = await db.StringGetAsync(key);
            if (cachedValue.HasValue)
            {
                var newValue = await getModified(cachedValue.FromValue<TData>(_settings._serializer));
                await db.StringSetAsync(key, newValue.ToRedisValue(_settings._serializer));

                return true;
            }

            return false;
        }
    }
}