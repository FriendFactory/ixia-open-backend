using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Infrastructure.Utils;
using Frever.Cache.Resetting;
using Frever.Cache.Supplement;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Frever.Cache.Strategies;

public class RedisPagedList<TItem>
{
    private readonly Type[] _globalDependencies;
    private readonly int _initialPageSize;
    private readonly int _pageSize;
    private readonly bool _reloadInitialData;
    private readonly SerializeAs? _serializer;
    private readonly Type[] _userDependencies;

    public RedisPagedList(
        SerializeAs? serializer,
        int pageSize,
        int initialPageSize,
        bool reloadInitialData,
        Type[] globalDependencies,
        Type[] userDependencies
    )
    {
        _serializer = serializer;
        _reloadInitialData = reloadInitialData;
        _globalDependencies = globalDependencies;
        _userDependencies = userDependencies;
        _pageSize = Math.Clamp(pageSize, 1, int.MaxValue);
        _initialPageSize = Math.Clamp(initialPageSize, 1, int.MaxValue);
    }

    public class Cache : IListCache<TItem>
    {
        private readonly ICurrentGroupAccessor _currentGroupAccessor;
        private readonly CacheDependencyTracker _dependencyTracker;
        private readonly IConnectionMultiplexer _redis;
        private readonly ILogger _redisLogger;
        private readonly RedisPagedList<TItem> _settings;

        public Cache(
            IConnectionMultiplexer redis,
            RedisPagedList<TItem> settings,
            CacheDependencyTracker dependencyTracker,
            ICurrentGroupAccessor currentGroupAccessor,
            ILoggerFactory loggerFactory
        )
        {
            if (loggerFactory == null)
                throw new ArgumentNullException(nameof(loggerFactory));
            _redis = redis ?? throw new ArgumentNullException(nameof(redis));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _dependencyTracker = dependencyTracker ?? throw new ArgumentNullException(nameof(dependencyTracker));
            _currentGroupAccessor = currentGroupAccessor ?? throw new ArgumentNullException(nameof(currentGroupAccessor));
            _redisLogger = loggerFactory.CreateLogger("Frever.Redis");
        }

        public async Task<List<TItem>> GetOrCache(
            string key,
            IListCache<TItem>.GetDataPage getData,
            int skip,
            int take,
            TimeSpan expiration
        )
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(key));

            var db = _redis.GetDatabase();

            var keyInCache = db.KeyExists(key);
            if (!keyInCache)
            {
                var size = Math.Max(_settings._initialPageSize, take + skip);

                var page = await getData(0, size);

                await db.ListRightPushAsync(key, page.Select(d => d.ToRedisValue(_settings._serializer)).ToArray());

                db.KeyExpire(key, expiration.Spread());

                if (_settings._globalDependencies != null)
                    await _dependencyTracker.Track(key, _settings._globalDependencies);

                if (_settings._userDependencies != null)
                {
                    var currentGroupId = _currentGroupAccessor.CurrentGroupId;
                    if (currentGroupId != null)
                        await _dependencyTracker.TrackUser(currentGroupId.Value, key, _settings._userDependencies);
                }

                return (await db.ListRangeAsync(key, skip, skip + take)).Select(r => r.FromValue<TItem>(_settings._serializer)).ToList();
            }

            var listCount = (int) await db.ListLengthAsync(key);

            if (_settings._reloadInitialData && listCount == _settings._initialPageSize)
            {
                _redisLogger.LogInformation("RedisPagedList.GetOrCache: deleting cache key {key} because of reloading initial data", key);
                db.KeyDelete(key);
                listCount = 0;
            }

            var requiredCount = skip + take;

            if (requiredCount > listCount)
            {
                // Fill up the cache
                var extraCount = requiredCount - listCount;
                var countToLoad = Math.Max(extraCount, _settings._pageSize);

                var page = await getData(listCount, countToLoad);
                await db.ListRightPushAsync(key, page.Select(d => d.ToRedisValue(_settings._serializer)).ToArray());
            }

            return (await db.ListRangeAsync(key, skip, skip + take)).Select(r => r.FromValue<TItem>(_settings._serializer)).ToList();
        }
    }
}