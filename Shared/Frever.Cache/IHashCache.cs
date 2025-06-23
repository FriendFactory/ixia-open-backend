using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Frever.Cache;

public interface IHashCache<TData>
    where TData : class, new()
{
    /// <summary>
    ///     Gets value from cache by specific key.
    ///     If value is not in cache or <paramref name="shouldRefresh" /> predicate returns true,
    ///     value would be re-read using <paramref name="get" /> function and cache would be updated.
    ///     If <paramref name="get" /> returns null then method would return null also and didn't cache anything.
    /// </summary>
    Task<TData> GetOrCache(string key, Func<Task<TData>> get, Predicate<TData> shouldRefresh, TimeSpan expiration);

    /// <summary>
    ///     Gets subset of values from cache at specific key.
    ///     Expression can be a single property expression or a new { A = x.B } construction.
    ///     If value is not in cache or <paramref name="shouldRefresh" /> predicate returns true,
    ///     value would be re-read using <paramref name="get" /> function and cache would be updated.
    ///     If <paramref name="get" /> returns null then method would return null also and didn't cache anything.
    /// </summary>
    Task<TResult> GetOrCache<TResult>(
        string key,
        Expression<Func<TData, TResult>> selector,
        Func<Task<TData>> get,
        Predicate<TResult> shouldRefresh,
        TimeSpan expiration
    );

    Task<List<TData>> GetByKeys(string[] keys);

    Task PutToCache(string key, Func<Task<TData>> get, TimeSpan expiration);

    Task DeleteFromCache(string key);

    /// <summary>
    ///     Increments specified property if object data are in cache.
    ///     If object are not in cache (ie GetOrCache were never called for the key)
    ///     then method do nothing to avoid storing only part of object properties in cache.
    /// </summary>
    Task Increment(string key, Expression<Func<TData, int>> prop, int by);

    Task Increment(string key, Expression<Func<TData, long>> prop, int by);

    /// <summary>
    ///     Sets one property in hash.
    ///     If object are not in cache (ie GetOrCache were never called for the key)
    ///     then method do nothing to avoid storing only part of object properties in cache.
    /// </summary>
    Task SetPropertyValue<TProp>(string key, Expression<Func<TData, TProp>> prop, TProp value);

    Task<Dictionary<TId, TData>> GetOrCacheMany<TId>(
        TId[] ids,
        Func<TId, string> getKey,
        Func<TId[], Task<Dictionary<TId, TData>>> getMissingData,
        TimeSpan expiration
    );
}