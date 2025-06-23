using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Common.Infrastructure.Caching.Advanced.SortedSet;

public interface ISortedSetCache
{
    Task AddRange<TElement>(string setKey, IEnumerable<TElement> elements, Func<TElement, long> getScore);

    Task<TElement> GetWithTheLargestScore<TElement>(string setKey);

    Task<TElement[]> GetRangeByScorePagedDesc<TElement>(
        string setKey,
        int skip = 0,
        int take = 50,
        long lowScore = long.MinValue,
        long highScore = long.MaxValue,
        Func<byte[], TElement> fromCache = null
    );

    Task DeleteElementByElementProperty<TElement>(
        string setKey,
        long propertyValue,
        Func<TElement, long> findByProperty,
        Func<TElement, long> removeByProperty
    );
}