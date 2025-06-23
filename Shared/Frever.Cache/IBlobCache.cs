using System;
using System.Threading.Tasks;

namespace Frever.Cache;

/// <summary>
///     Cache that stores unstructured data.
///     The data could only be fully extracted from cache by key.
/// </summary>
public interface IBlobCache<TData>
{
    Task<TData> GetOrCache(string key, Func<Task<TData>> getData, TimeSpan expiration);

    Task<TData> TryGet(string key);

    Task<bool> TryModifyInPlace(string key, Func<TData, Task<TData>> getModified);
}