using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Common.Infrastructure.Utils;
using Frever.Cache.Resetting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Frever.Cache.Strategies;

public class RedisDictionary<TId, TData>
{
    private readonly string _baseKey;

    private readonly Type[] _dependencies;
    private readonly SerializeAs? _serializer;

    public RedisDictionary(string baseKey, Type[] dependencies, SerializeAs? serializer)
    {
        if (string.IsNullOrWhiteSpace(baseKey))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(baseKey));
        _baseKey = baseKey;
        _dependencies = dependencies ?? throw new ArgumentNullException(nameof(dependencies));
        _serializer = serializer;
    }

    public class Cache(
        IConnectionMultiplexer redis,
        RedisDictionary<TId, TData> settings,
        CacheDependencyTracker cacheDependencyTracker,
        ILoggerFactory loggerFactory
    ) : IDictionaryCache<TId, TData>
    {
        private readonly CacheDependencyTracker _cacheDependencyTracker = cacheDependencyTracker ?? throw new ArgumentNullException(nameof(cacheDependencyTracker));
        private readonly IConnectionMultiplexer _redis = redis ?? throw new ArgumentNullException(nameof(redis));
        private readonly ILogger _redisLogger = loggerFactory.CreateLogger("Frever.Redis");
        private readonly RedisDictionary<TId, TData> _settings = settings ?? throw new ArgumentNullException(nameof(settings));

        public async Task<TData> GetOrCache(TId id, Func<TId, Task<TData>> getData, TimeSpan expiration)
        {
            ArgumentNullException.ThrowIfNull(getData);

            var db = _redis.GetDatabase();

            var key = KeyById(id);

            var redisValue = await db.StringGetAsync(key);

            if (redisValue == RedisValue.Null)
            {
                var value = await getData(id);
                await db.StringSetAsync(key, value.ToRedisValue(_settings._serializer));

                await _cacheDependencyTracker.Track(key, _settings._dependencies);
                await db.KeyExpireAsync(key, expiration.Spread());

                return value;
            }

            return redisValue.FromValue<TData>(_settings._serializer);
        }

        public async Task<IDictionary<TId, TData>> GetCachedData(TId[] keys)
        {
            ArgumentNullException.ThrowIfNull(keys);

            var db = _redis.GetDatabase();

            IDictionary<TId, TData> result = new Dictionary<TId, TData>();

            foreach (var key in keys)
            {
                var redisValue = await db.StringGetAsync(KeyById(key));
                if (redisValue == RedisValue.Null)
                    continue;

                result[key] = redisValue.FromValue<TData>(_settings._serializer);
            }

            return result;
        }

        public Task PutToCache(IDictionary<TId, TData> data, TimeSpan expiration)
        {
            ArgumentNullException.ThrowIfNull(data);

            var db = _redis.GetDatabase();

            foreach (var (key, value) in data)
                db.StringSet(KeyById(key), value.ToRedisValue(), expiration.Spread());

            return Task.CompletedTask;
        }

        public async Task<bool> TryModifyInPlace(TId id, Func<TData, Task<TData>> getModified)
        {
            ArgumentNullException.ThrowIfNull(getModified);

            var key = KeyById(id);

            var db = _redis.GetDatabase();

            var redisValue = await db.StringGetAsync(key);

            if (redisValue == RedisValue.Null)
                return false;

            var value = redisValue.FromValue<TData>(_settings._serializer);
            var newValue = await getModified(value);

            await db.StringSetAsync(key, newValue.ToRedisValue(_settings._serializer));

            return true;
        }

        public async Task<bool> AddOrUpdate(TId id, TimeSpan expiration, Func<TData, Task<TData>> getOrModify)
        {
            ArgumentNullException.ThrowIfNull(getOrModify);

            var key = KeyById(id);

            var db = _redis.GetDatabase();

            var redisValue = await db.StringGetAsync(key);

            if (redisValue != RedisValue.Null)
            {
                var value = redisValue.FromValue<TData>(_settings._serializer);
                var newValue = await getOrModify(value);

                await db.StringSetAsync(key, newValue.ToRedisValue(_settings._serializer));
                db.KeyExpire(key, expiration.Spread());

                return true;
            }
            else
            {
                var newValue = await getOrModify(default);
                await db.StringSetAsync(key, newValue.ToRedisValue(_settings._serializer));
                db.KeyExpire(key, expiration.Spread());

                return false;
            }
        }

        public Task RemoveByKey(TId id)
        {
            var db = _redis.GetDatabase();

            var key = KeyById(id);

            _redisLogger.LogInformation("RedisDictionary.RemoveByKey({id}): deleting cache key {key}", id, key);

            return db.KeyDeleteAsync(key);
        }

        private string KeyById(TId id)
        {
            return $"{_settings._baseKey}::{id}";
        }
    }
}