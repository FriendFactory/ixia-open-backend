using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Infrastructure.Caching.CacheKeys;
using Common.Infrastructure.Utils;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace Common.Infrastructure.Caching;

public sealed class RedisCache : ICache
{
    private static readonly string[] KeysToIgnoreOnReset =
    [
        VideoCacheKeys.SharedPrefix.GetKeyWithoutVersion(),
        VideoCacheKeys.ConversionPrefix,
        NotificationCacheKeys.NotificationPerInstancePrefix,
        AiContentCacheKeys.AiContentGenerationPrefix
    ];

    private readonly IConnectionMultiplexer _connection;
    private readonly ILogger _logger;
    private readonly RedisSettings _settings;

    public RedisCache(IConnectionMultiplexer connection, RedisSettings settings, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _logger = loggerFactory.CreateLogger("Frever.RedisCache");
    }

    public bool IsEnabled => _settings.EnableCaching;

    public string ClientIdentifier => _settings.ClientIdentifier;

    public async Task<T> TryGet<T>(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(key));

        var db = Db();
        var result = await db.StringGetAsync(key);

        TryConvertFromRedisValue<T>(result, out var value);

        return value;
    }

    public async Task<T[]> TryGetMany<T>(string[] keys)
    {
        if (keys == null)
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(keys));

        var db = Db();
        var cached = await db.StringGetAsync(keys.Select(k => (RedisKey) k).ToArray());

        var result = new List<T>();

        foreach (var item in cached)
            if (TryConvertFromRedisValue<T>(item, out var value))
                result.Add(value);

        return result.ToArray();
    }

    public async Task<T> GetOrCache<T>(string key, Func<Task<T>> getValue, TimeSpan? expiration = null)
    {
        if (getValue == null)
            throw new ArgumentNullException(nameof(getValue));
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(key));

        var db = Db();

        var value = default(T);
        var redisValue = await db.StringGetAsync(key);

        if (TryConvertFromRedisValue(redisValue, out value))
            return value;

        value = await getValue();

        await db.StringSetAsync(key, ToCache(value), expiration.Spread());

        return value;
    }

    /// <summary>
    ///     Tries to get values by keys from hash with specified name.
    ///     If some values are not in cache call the <paramref name="getValues" /> function
    ///     to get the data from external and cache it.
    /// </summary>
    public async Task<IDictionary<TId, TValue>> GetOrCacheFromHash<TValue, TId>(
        string hashName,
        Func<TId[], Task<TValue[]>> getValues,
        Func<TValue, TId> getKey,
        params TId[] ids
    )
        where TId : IConvertible
    {
        if (getValues == null)
            throw new ArgumentNullException(nameof(getValues));
        if (string.IsNullOrWhiteSpace(hashName))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(hashName));

        if (ids == null || ids.Length == 0)
            return new Dictionary<TId, TValue>();

        var db = Db();

        var values = await db.HashGetAsync(hashName, ids.Select<TId, RedisValue>(i => i.ToString()).ToArray());

        var result = new Dictionary<TId, TValue>();
        var missingKeys = new List<TId>();

        for (var i = 0; i < values.Length; i++)
        {
            var id = ids[i];
            var value = values[i];
            if (!value.HasValue)
            {
                missingKeys.Add(id);
            }
            else
            {
                if (TryConvertFromRedisValue<TValue>(value, out var val))
                    result[id] = val;
            }
        }

        if (missingKeys.Count > 0)
        {
            var missingValues = await getValues(missingKeys.ToArray());
            foreach (var value in missingValues)
            {
                var key = getKey(value);

                db.HashSet(hashName, key.ToString(), ToCache(value));
                result[key] = value;
            }
        }

        return result;
    }

    public Task SetExpire(string key, TimeSpan expiration)
    {
        return Db().KeyExpireAsync(key, expiration.Spread());
    }

    public Task<bool> HasKey(string key)
    {
        return Db().KeyExistsAsync(key);
    }

    public async Task ClearCache()
    {
        var allKeys = await GetKeysByPrefix("*");

        var keys = allKeys.Where(e => KeysToIgnoreOnReset.All(k => !e.Contains(k))).ToArray();

        await DeleteKeys(keys);
    }

    public async Task<string[]> GetKeysByPrefix(string keyPrefix)
    {
        if (string.IsNullOrWhiteSpace(keyPrefix))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(keyPrefix));

        var pattern = keyPrefix.TrimEnd('*') + '*';

        return await GetKeys(pattern);
    }

    public async Task<string[]> GetKeysByInfix(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(value));

        var pattern = '*' + value.TrimStart('*').TrimEnd('*') + '*';

        return await GetKeys(pattern);
    }

    public async Task Put<T>(string key, T value, TimeSpan? expiration = null)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(key));

        var json = ToCache(value);
        await Db().StringSetAsync(key, json, expiration.Spread());
    }

    public Task<bool> DeleteKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentNullException(nameof(key));

        return Db().KeyDeleteAsync(key);
    }

    public async Task DeleteKeys(params string[] keys)
    {
        if (keys == null)
            throw new ArgumentNullException(nameof(keys));

        if (keys.Length == 0)
            return;

        if (keys.Length == 1)
        {
            _logger.LogInformation("Deleting redis key {0}", keys[0]);
            await Db().KeyDeleteAsync(keys[0], CommandFlags.DemandMaster);
        }
        else
        {
            var redisKeys = keys.Select(k => (RedisKey) k).ToArray();
            await Task.WhenAll(
                redisKeys.Select(
                    k =>
                    {
                        _logger.LogInformation("Deleting redis key {0}", k);
                        return Db().KeyDeleteAsync(k, CommandFlags.DemandMaster);
                    }
                )
            );
        }
    }

    public async Task DeleteKeysWithPrefix(string keyPrefix)
    {
        if (string.IsNullOrWhiteSpace(keyPrefix))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(keyPrefix));

        await DeleteKeys(await GetKeysByPrefix(keyPrefix));
    }

    public async Task DeleteKeysWithInfix(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(value));

        await DeleteKeys(await GetKeysByInfix(value));
    }

    public IDatabase Db()
    {
        return _connection.GetDatabase();
    }

    public IServer Server()
    {
        var endpoints = _connection.GetEndPoints();
        var main = endpoints.First();

        return _connection.GetServer(main);
    }

    private async Task<string[]> GetKeys(string pattern)
    {
        var keysAcc = new List<string>();

        var server = Server();
        await foreach (var k in server.KeysAsync(pattern: pattern))
            keysAcc.Add(k);

        return keysAcc.ToArray();
    }

    private RedisValue ToCache<T>(T value)
    {
        return JsonConvert.SerializeObject(value, new JsonSerializerSettings {ReferenceLoopHandling = ReferenceLoopHandling.Ignore});
    }

    private T FromCache<T>(RedisValue value)
    {
        return JsonConvert.DeserializeObject<T>(value);
    }

    private bool TryConvertFromRedisValue<T>(RedisValue value, out T result)
    {
        result = default;

        if (!value.HasValue || value.IsNullOrEmpty)
            return false;

        result = FromCache<T>(value);

        return true;
    }
}