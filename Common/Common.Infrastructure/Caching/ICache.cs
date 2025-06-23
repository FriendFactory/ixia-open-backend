using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace Common.Infrastructure.Caching;

public interface ICache
{
    bool IsEnabled { get; }

    string ClientIdentifier { get; }

    /// <summary>
    ///     Tries to read data from cache.
    ///     If value is not found or cache is not accessible, returns null (or default value for T).
    /// </summary>
    Task<T> TryGet<T>(string key);

    Task<T[]> TryGetMany<T>(string[] keys);

    /// <summary>
    ///     Tries to read data from cache.
    ///     If value is not presented in cache, calls getValue to get value and put it to cache.
    /// </summary>
    Task<T> GetOrCache<T>(string key, Func<Task<T>> getValue, TimeSpan? expiration = null);

    /// <summary>
    ///     Gets keys in cache with specified prefix.
    /// </summary>
    Task<string[]> GetKeysByPrefix(string keyPrefix);

    /// <summary>
    ///     Gets keys in cache contains specified prefix.
    /// </summary>
    Task<string[]> GetKeysByInfix(string value);

    /// <summary>
    ///     Creates or updates value in cache.
    /// </summary>
    Task Put<T>(string key, T value, TimeSpan? expiration = null);

    /// <summary>
    ///     Remove specified keys from cache.
    /// </summary>
    Task DeleteKeys(params string[] keys);

    Task<bool> DeleteKey(string key);

    /// <summary>
    ///     Removes all keys started with prefix
    /// </summary>
    Task DeleteKeysWithPrefix(string keyPrefix);

    /// <summary>
    ///     Removes all keys contains infix
    /// </summary>
    Task DeleteKeysWithInfix(string value);

    /// <summary>
    ///     Tries to get values by keys from hash with specified name.
    ///     If some values are not in cache call the <paramref name="getValues" /> function
    ///     to get the data from external and cache it.
    /// </summary>
    Task<IDictionary<TId, TValue>> GetOrCacheFromHash<TValue, TId>(
        string hashName,
        Func<TId[], Task<TValue[]>> getValues,
        Func<TValue, TId> getKey,
        params TId[] ids
    )
        where TId : IConvertible;

    Task ClearCache();

    Task SetExpire(string key, TimeSpan expiration);

    Task<bool> HasKey(string key);

    IDatabase Db();

    IServer Server();
}