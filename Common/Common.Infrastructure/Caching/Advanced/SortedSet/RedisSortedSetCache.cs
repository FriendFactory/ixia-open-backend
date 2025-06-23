using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace Common.Infrastructure.Caching.Advanced.SortedSet;

public class RedisSortedSetCache(IConnectionMultiplexer connection) : ISortedSetCache
{
    private static readonly JsonSerializerSettings SerializerSettings =
        new() {TypeNameHandling = TypeNameHandling.All, TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple};

    private readonly IConnectionMultiplexer _connection = connection ?? throw new ArgumentNullException(nameof(connection));

    public Task AddRange<TElement>(string setKey, IEnumerable<TElement> elements, Func<TElement, long> getScore)
    {
        ArgumentNullException.ThrowIfNull(elements);

        if (string.IsNullOrEmpty(setKey))
            throw new ArgumentException("Value cannot be null or empty.", nameof(setKey));

        return Db().SortedSetAddAsync(setKey, elements.Select(e => new SortedSetEntry(ToCache(e), getScore(e))).ToArray());
    }

    public async Task DeleteElementByElementProperty<TElement>(
        string setKey,
        long propertyValue,
        Func<TElement, long> findByProperty,
        Func<TElement, long> removeByProperty
    )
    {
        if (string.IsNullOrWhiteSpace(setKey))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(setKey));

        var type = await Db().KeyTypeAsync(setKey);
        if (type != RedisType.SortedSet)
            return;

        var all = await Db().SortedSetRangeByScoreAsync(setKey);

        var value = all.Where(e => e.HasValue)
                       .Select(e => TryConvertFromRedisValue(e, out TElement item) ? item : default)
                       .FirstOrDefault(e => findByProperty(e) == propertyValue);

        if (value == null)
            return;

        var score = removeByProperty(value);

        await Db().SortedSetRemoveRangeByScoreAsync(setKey, score, score);
    }

    public async Task<TElement> GetWithTheLargestScore<TElement>(string setKey)
    {
        if (string.IsNullOrWhiteSpace(setKey))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(setKey));

        var values = await Db()
                    .SortedSetRangeByScoreAsync(
                         setKey,
                         double.PositiveInfinity,
                         0,
                         take: 1,
                         order: Order.Descending
                     );

        if (values.Length == 0)
            return default;

        return TryConvertFromRedisValue(values.First(), out TElement item) ? item : default;
    }

    public async Task<TElement[]> GetRangeByScorePagedDesc<TElement>(
        string setKey,
        int skip = 0,
        int take = 50,
        long lowScore = long.MinValue,
        long highScore = long.MaxValue,
        Func<byte[], TElement> fromCache = null
    )
    {
        var cachedItems = await Db()
                         .SortedSetRangeByScoreAsync(
                              setKey,
                              highScore,
                              lowScore,
                              take: take,
                              skip: skip,
                              order: Order.Descending
                          );

        fromCache ??= FromCache<TElement>;

        return cachedItems.Where(e => e.HasValue).Select(e => fromCache(e)).Where(e => e != null).ToArray();
    }

    private IDatabase Db()
    {
        return _connection.GetDatabase();
    }

    private byte[] ToCache<T>(T value)
    {
        return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(value, SerializerSettings));
    }

    private T FromCache<T>(byte[] value)
    {
        return JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(value), SerializerSettings);
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