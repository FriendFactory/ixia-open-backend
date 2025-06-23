using System;
using System.Threading.Tasks;
using Common.Infrastructure;
using Common.Infrastructure.Caching;
using Common.Infrastructure.Caching.Advanced.SortedList;
using Common.Infrastructure.Caching.CacheKeys;
using Common.Infrastructure.RequestId;
using Microsoft.Extensions.Logging;

namespace Frever.Video.Core.Features.PersonalFeed;

public interface IPersonalFeedRefreshingService
{
    Task RefreshFeed(long groupId, decimal lon, decimal lat);
}

public class MLPersonalFeedRefreshingService(
    ICache cache,
    ILogger<MLPersonalFeedService> log,
    IPersonalFeedGenerator personalFeedBuilder,
    ISortedListCache sortedListCache,
    IHeaderAccessor headerAccessor
) : IPersonalFeedRefreshingService
{
    public async Task RefreshFeed(long groupId, decimal lon, decimal lat)
    {
        using var scope = log.BeginScope("Refreshing ML feed for {GroupId}", groupId);
        using var sw = log.LogTime(TimeSpan.FromSeconds(5), "Refreshing ML feed took {0} for group {1}", groupId);

        var headers = headerAccessor.GetRequestExperimentsHeader();
        var result = await personalFeedBuilder.GenerateFeed(groupId, headers, lon, lat);

        var expiration = TimeSpan.FromDays(10);
        var latestVersion = await cache.TryGet<int>(VideoCacheKeys.MlPoweredPersonalVideoFeedVersion(groupId));

        var nextVersion = latestVersion + 1;
        await sortedListCache.SetRange(
            VideoCacheKeys.MlPoweredPersonalVideoFeed(groupId, nextVersion),
            result,
            vr => vr.SortOrder,
            true,
            expiration
        );

        await cache.Put(VideoCacheKeys.MlPoweredPersonalVideoFeedVersion(groupId), nextVersion, expiration);

        log.LogInformation(
            "ML feed updated with {Length} items. Cache key {Version}",
            result.Length,
            VideoCacheKeys.MlPoweredPersonalVideoFeed(groupId, nextVersion)
        );

        await cache.SetExpire(VideoCacheKeys.MlPoweredPersonalVideoFeed(groupId, latestVersion), TimeSpan.FromMinutes(5));
    }
}