using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AuthServer.Permissions.Services;
using AuthServerShared;
using Common.Infrastructure.Caching;
using Common.Infrastructure.Caching.Advanced.SortedList;
using Common.Infrastructure.Caching.CacheKeys;
using Frever.Client.Shared.Social.Services;
using Frever.Shared.MainDb.Entities;
using Frever.Video.Contract;
using Frever.Videos.Shared.MusicGeoFiltering;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Frever.Video.Core.Features.PersonalFeed;

public interface IPersonalFeedService
{
    Task<(VideoInfo[] feed, int version)> PersonalFeed(long groupId, string targetVideo, int takeNext);
}

public class MLPersonalFeedService(
    IUserPermissionService userPermissionService,
    UserInfo currentUser,
    ICache cache,
    ILogger<MLPersonalFeedService> log,
    ICurrentLocationProvider locationProvider,
    IPersonalFeedRefreshingService feedRefreshing,
    ISocialSharedService socialSharedService,
    IVideoLoader videoLoader,
    ISortedListCache sortedListCache
) : IPersonalFeedService
{
    public async Task<(VideoInfo[] feed, int version)> PersonalFeed(long groupId, string targetVideo, int takeNext)
    {
        await userPermissionService.EnsureCurrentUserActive();

        var version = await PersonalFeedVersion(groupId);
        var feedKey = VideoCacheKeys.MlPoweredPersonalVideoFeed(groupId, version);

        if (!await cache.HasKey(feedKey))
        {
            log.LogWarning("FYP CACHE IS EMPTY, REBUILDING. GroupId={GroupId}", groupId);
            var sw = Stopwatch.StartNew();
            var location = await locationProvider.Get();
            await feedRefreshing.RefreshFeed(groupId, location.Lon, location.Lat);

            sw.Stop();
            log.LogInformation("FYP REFRESHING TOOK {Elapsed}", sw.Elapsed);

            version = await PersonalFeedVersion(groupId);
            feedKey = VideoCacheKeys.MlPoweredPersonalVideoFeed(groupId, version);
        }

        var result = await GetCachedVideosFromSortedCache(feedKey, targetVideo, takeNext);

        if (result.Length == 0)
            log.LogWarning(
                "WARNING: EMPTY ML FYP PAGE. GroupId={GroupId} targetVideo={TargetVideo} takeNext={TakeNext}",
                groupId,
                targetVideo,
                takeNext
            );

        return (result, version);
    }


    private Task<int> PersonalFeedVersion(long groupId)
    {
        return cache.TryGet<int>(VideoCacheKeys.MlPoweredPersonalVideoFeedVersion(groupId));
    }

    private async Task<VideoInfo[]> GetCachedVideosFromSortedCache(string videoListKey, string targetVideo, int takeNext)
    {
        var target = long.TryParse(targetVideo, out var key) ? key : (long?) null;

        var blockedGroups = await socialSharedService.GetBlocked(currentUser);

        var result = await videoLoader.LoadVideoPage(
                         FetchVideoInfoFrom.ReadDb,
                         async (target, takeNext, _) => (await sortedListCache.GetRange<VideoRef>(
                                                             videoListKey,
                                                             v => v.SortOrder,
                                                             v => Task.FromResult(
                                                                 v.Where(r => !blockedGroups.Contains(r.GroupId)).ToArray()
                                                             ),
                                                             target,
                                                             takeNext
                                                         )).Select(
                                                                r => new VideoWithSong
                                                                     {
                                                                         Id = r.Id,
                                                                         Key = r.SortOrder,
                                                                         SongInfo = JsonConvert.SerializeObject(r.SongInfo)
                                                                     }
                                                            )
                                                           .ToList(),
                         Sorting.Desc,
                         target == null ? string.Empty : target.ToString(),
                         takeNext
                     );

        return result;
    }
}