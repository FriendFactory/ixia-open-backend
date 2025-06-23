using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Infrastructure.Utils;
using Frever.Cache.Resetting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Frever.Cache.Strategies;

public class InMemoryDictionary<TId, TData>
{
    private const int ExpirationDays = 7;
    private const int RefreshIntervalDays = 1;
    private static readonly ConcurrentDictionary<TId, Item<TData>> CacheContainer = new();
    private static Timer _timer;

    private readonly string _baseKey;
    private readonly Guid _cacheGuid;
    private readonly bool _cloneNonCachedValue;
    private readonly Type[] _dependencies;


    public InMemoryDictionary(string baseKey, Type[] dependencies, bool cloneNonCachedValue, bool removeExpiredValues)
    {
        if (string.IsNullOrWhiteSpace(baseKey))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(baseKey));

        _baseKey = baseKey;
        _dependencies = dependencies ?? throw new ArgumentNullException(nameof(dependencies));
        _cloneNonCachedValue = cloneNonCachedValue;
        _cacheGuid = Guid.NewGuid();

        if (removeExpiredValues)
            _timer = new Timer(DeleteExpiredItems, null, TimeSpan.Zero, TimeSpan.FromDays(RefreshIntervalDays));
    }

    private void DeleteExpiredItems(object state)
    {
        if (CacheContainer.IsEmpty)
            return;

        var expiredItems = CacheContainer.Where(e => e.Value.AddedAt.AddDays(ExpirationDays) < DateTimeOffset.Now).ToArray();
        if (!expiredItems.Any())
            return;

        foreach (var item in expiredItems)
            CacheContainer.Remove(item.Key, out _);
    }

    public class Cache : IDictionaryCache<TId, TData>
    {
        private readonly CacheDependencyTracker _cacheDependencyTracker;
        private readonly IConnectionMultiplexer _redis;
        private readonly ILogger _redisLogger;
        private readonly InMemoryDictionary<TId, TData> _settings;

        public Cache(
            IConnectionMultiplexer redis,
            InMemoryDictionary<TId, TData> settings,
            CacheDependencyTracker cacheDependencyTracker,
            ILoggerFactory loggerFactory
        )
        {
            ArgumentNullException.ThrowIfNull(loggerFactory);

            _redis = redis ?? throw new ArgumentNullException(nameof(redis));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _cacheDependencyTracker = cacheDependencyTracker ?? throw new ArgumentNullException(nameof(cacheDependencyTracker));
            _redisLogger = loggerFactory.CreateLogger("Frever.Redis");
        }

        public async Task<TData> GetOrCache(TId id, Func<TId, Task<TData>> getData, TimeSpan expiration)
        {
            if (getData == null)
                throw new ArgumentNullException(nameof(getData));

            var db = _redis.GetDatabase();

            var key = KeyById(id);

            var isCacheReset = await db.StringGetAsync(key) == RedisValue.Null;
            var isCached = CacheContainer.TryGetValue(id, out var cachedValue);

            if (isCacheReset || !isCached)
            {
                var data = await getData(id);
                if (data == null)
                    return default;

                if (_settings._cloneNonCachedValue)
                    data = data.ToRedisValue().FromValue<TData>();

                await db.StringSetAsync(key, true.ToRedisValue(), expiration.Spread());
                await _cacheDependencyTracker.Track(key, _settings._dependencies);

                CacheContainer[id] = new Item<TData>(data);

                return data;
            }

            return cachedValue.Value;
        }

        public async Task<IDictionary<TId, TData>> GetCachedData(TId[] keys)
        {
            if (keys == null)
                throw new ArgumentNullException(nameof(keys));

            var db = _redis.GetDatabase();

            var result = new Dictionary<TId, TData>();

            foreach (var key in keys)
            {
                var isCacheReset = await db.StringGetAsync(KeyById(key)) == RedisValue.Null;
                if (isCacheReset)
                    continue;

                if (CacheContainer.TryGetValue(key, out var data))
                    result[key] = data.Value;
            }

            return result;
        }

        public async Task PutToCache(IDictionary<TId, TData> data, TimeSpan expiration)
        {
            var db = _redis.GetDatabase();

            foreach (var (id, value) in data)
            {
                var cacheKey = KeyById(id);

                await db.StringSetAsync(cacheKey, true.ToRedisValue(), expiration.Spread());

                await _cacheDependencyTracker.Track(cacheKey, _settings._dependencies);

                CacheContainer[id] = new Item<TData>(value);
            }
        }

        public async Task<bool> TryModifyInPlace(TId id, Func<TData, Task<TData>> getModified)
        {
            if (getModified == null)
                throw new ArgumentNullException(nameof(getModified));

            var db = _redis.GetDatabase();

            var key = KeyById(id);

            var isCacheReset = await db.StringGetAsync(key) == RedisValue.Null;
            var isCached = CacheContainer.TryGetValue(id, out var cachedValue);

            if (isCached && !isCacheReset)
            {
                var newValue = await getModified(cachedValue.Value);
                CacheContainer[id] = new Item<TData>(newValue);
                return true;
            }

            return false;
        }

        public async Task<bool> AddOrUpdate(TId id, TimeSpan expiration, Func<TData, Task<TData>> getOrModify)
        {
            ArgumentNullException.ThrowIfNull(getOrModify);

            var db = _redis.GetDatabase();

            var key = KeyById(id);

            var isCacheReset = await db.StringGetAsync(key) == RedisValue.Null;
            var isCached = CacheContainer.TryGetValue(id, out var cachedValue);

            if (isCached && !isCacheReset)
            {
                var newValue = await getOrModify(cachedValue.Value);
                CacheContainer[id] = new Item<TData>(newValue);
                return true;
            }
            else
            {
                var newValue = await getOrModify(default);
                CacheContainer[id] = new Item<TData>(newValue);
                return false;
            }
        }

        public async Task RemoveByKey(TId id)
        {
            var db = _redis.GetDatabase();

            var pattern = $"{{{_settings._baseKey}::{id}}}*";

            var server = Server();

            var keys = server.Keys(pattern: pattern);
            var redisKeys = keys as RedisKey[] ?? keys.ToArray();
            _redisLogger.LogInformation(
                "InMemoryDictionary.RemoveByKey({id}): deleting cache keys {keys}",
                id,
                string.Join(", ", redisKeys)
            );
            await db.KeyDeleteAsync(redisKeys, CommandFlags.DemandMaster);
        }

        private IServer Server()
        {
            var endpoints = _redis.GetEndPoints();
            var main = endpoints.First();

            return _redis.GetServer(main);
        }

        private string KeyById(TId id)
        {
            return $"{{{_settings._baseKey}::{id}}}/{_settings._cacheGuid}";
        }
    }
}

public class Item<TData>
{
    public Item(TData value)
    {
        Value = value;
    }

    public TData Value { get; }
    internal DateTimeOffset AddedAt { get; } = DateTimeOffset.Now;
}