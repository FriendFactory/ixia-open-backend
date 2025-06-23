using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Infrastructure.Caching.Advanced.SortedSet;
using Common.Infrastructure.Utils;
using Microsoft.Extensions.Logging;

namespace Common.Infrastructure.Caching.Advanced.SortedList;

/// <summary>
///     Stores a range of any item in cache.
/// </summary>
public class SortedListCache(ICache cache, ISortedSetCache sortedSetCache, ILogger<SortedListCache> logger) : ISortedListCache
{
    private readonly ICache _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    private readonly ISortedSetCache _sortedSetCache = sortedSetCache ?? throw new ArgumentNullException(nameof(sortedSetCache));

    /// <summary>
    ///     Set a range of items from cache sorted by score in descending order.
    /// </summary>
    /// <param name="cacheKey">Key to distinguish sets in cache</param>
    /// <param name="source">Source of data.</param>
    /// <param name="score">Expression to obtain score value from item.</param>
    /// <param name="resetKey">Flag to check if key need to be reset before adding new values</param>
    /// <param name="expiration">Set expiration time</param>
    public async Task SetRange<T>(
        string cacheKey,
        IEnumerable<T> source,
        Func<T, long> score,
        bool resetKey,
        TimeSpan expiration
    )
        where T : class
    {
        if (resetKey)
            await _cache.DeleteKeys(cacheKey);

        await _sortedSetCache.AddRange(cacheKey, source.OrderByDescending(score), score);

        await _cache.SetExpire(cacheKey, expiration.Spread());
    }

    /// <summary>
    ///     Delete item from cache by item property.
    /// </summary>
    /// <param name="cacheKey">Key to distinguish sets in cache</param>
    /// <param name="propertyValue">Value of property by which element is searched</param>
    /// <param name="findByProperty">Property by which element is searched</param>
    /// <param name="removeByProperty">Property by which element is removed</param>
    /// <returns>Range of T ordered by score in descending order</returns>
    public Task DeleteByElementProperty<T>(
        string cacheKey,
        long propertyValue,
        Func<T, long> findByProperty,
        Func<T, long> removeByProperty
    )
    {
        return _sortedSetCache.DeleteElementByElementProperty(cacheKey, propertyValue, findByProperty, removeByProperty);
    }

    public async Task<IEnumerable<T>> GetRange<T>(
        string cacheKey,
        Func<T, long> getScore,
        Func<T[], Task<T[]>> filterValues,
        long? targetScore,
        int count
    )
        where T : class
    {
        ArgumentNullException.ThrowIfNull(getScore);
        ArgumentNullException.ThrowIfNull(filterValues);

        if (string.IsNullOrWhiteSpace(cacheKey))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(cacheKey));

        if (count == 0 || targetScore < 0)
        {
            logger.LogInformation("Target score is end the end or count is zero, returning empty set");
            return [];
        }

        if (targetScore == null)
        {
            var item = await _sortedSetCache.GetWithTheLargestScore<T>(cacheKey);

            // Cache is empty
            if (item == null)
            {
                logger.LogInformation("Cache is empty");
                return [];
            }

            var score = getScore(item);

            logger.LogInformation("Target score were null, get highest score {Score} and do recursive call", score);
            return await GetRange(
                       cacheKey,
                       getScore,
                       filterValues,
                       score,
                       count
                   );
        }

        var higher = targetScore.Value;
        var lower = higher - count - 1;

        var values = await _sortedSetCache.GetRangeByScorePagedDesc<T>(cacheKey, 0, count, highScore: higher);

        if (values.Length == 0)
        {
            logger.LogInformation("No values were loaded from cache in range {Higher}-{Lower}. Probably end of cache", higher, lower);

            return values;
        }

        var availableValues = await filterValues(values);

        logger.LogInformation(
            "Range were read from cache: total {ValuesLength}, non-filtered out: {AvailableValuesLength}",
            values.Length,
            availableValues.Length
        );

        if (availableValues.Length == 0 && lower == 0)
        {
            logger.LogInformation("No values were loaded. Probably end of cache");

            return availableValues;
        }

        if (availableValues.Length < count)
        {
            var lowerScore = values.Min(getScore);

            logger.LogInformation(
                "Values were not fully loaded. Requested next {AvailableValuesLength} starting from score {LowerScore}",
                count - availableValues.Length,
                lowerScore - 1
            );

            var restValues = (await GetRange(
                                  cacheKey,
                                  getScore,
                                  filterValues,
                                  lowerScore - 1,
                                  count - availableValues.Length
                              )).ToArray();

            logger.LogInformation(
                "Rest of values loaded (requested {Count}): {RestValuesLength}, total {AvailableValuesLength}",
                count,
                restValues.Length,
                availableValues.Length + restValues.Length
            );
            return availableValues.Concat(restValues);
        }

        logger.LogInformation("Values fully loaded requested: {Count} loaded {AvailableValuesLength}", count, availableValues.Length);

        return availableValues;
    }
}