using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Frever.Cache;

/// <summary>
///     Cache that stores an list of values ordered by source order.
///     Allows getting page of data by skip and top.
/// </summary>
public interface IListCache<TItem>
{
    public delegate Task<TItem[]> GetDataPage(int skip, int top);

    /// <summary>
    ///     Gets the page of TItem from cache.
    ///     If cache doesn't contain requested page it will be loaded with <paramref name="getData" /> delegate.
    /// </summary>
    /// <param name="key">Key to cache.</param>
    /// <param name="getData">Delegate to request data from underlying data source.</param>
    /// <param name="skip">Number of items to skip.</param>
    /// <param name="take">Number of items to read from cache.</param>
    /// <param name="expiration">Cached data lifetime</param>
    Task<List<TItem>> GetOrCache(
        string key,
        GetDataPage getData,
        int skip,
        int take,
        TimeSpan expiration
    );
}