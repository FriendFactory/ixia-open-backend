using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Frever.Cache;

/// <summary>
///     Cache that stores data accessible by id.
/// </summary>
public interface IDictionaryCache<TId, TData>
{
    Task<TData> GetOrCache(TId id, Func<TId, Task<TData>> getData, TimeSpan expiration);

    /// <summary>
    ///     Gets the multiple values from cache by id.
    ///     If id is not in cache it would be skipped.
    /// </summary>
    Task<IDictionary<TId, TData>> GetCachedData(TId[] keys);

    /// <summary>
    ///     Puts a set of values to cache.
    /// </summary>
    Task PutToCache(IDictionary<TId, TData> data, TimeSpan expiration);

    /// <summary>
    ///     Updates a value if value is already in cache.
    /// </summary>
    Task<bool> TryModifyInPlace(TId id, Func<TData, Task<TData>> getModified);

    /// <summary>
    ///     Adds value to cache or updates an existing value.
    /// </summary>
    Task<bool> AddOrUpdate(TId id, TimeSpan expiration, Func<TData, Task<TData>> getOrModify);

    Task RemoveByKey(TId id);
}