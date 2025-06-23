using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Common.Infrastructure.Caching.Advanced.SortedList;

public interface ISortedListCache
{
    Task SetRange<T>(
        string cacheKey,
        IEnumerable<T> source,
        Func<T, long> score,
        bool resetKey,
        TimeSpan expiration
    )
        where T : class;

    Task<IEnumerable<T>> GetRange<T>(
        string cacheKey,
        Func<T, long> getScore,
        Func<T[], Task<T[]>> filterValues,
        long? targetScore,
        int count
    )
        where T : class;
}