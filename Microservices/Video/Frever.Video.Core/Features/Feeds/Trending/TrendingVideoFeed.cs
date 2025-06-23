using System.Threading.Tasks;
using AuthServer.Permissions.Services;
using AuthServerShared;
using Frever.Video.Contract;
using Frever.Videos.Shared.GeoClusters;
using Microsoft.Extensions.Logging;

namespace Frever.Video.Core.Features.Feeds.Trending;

public interface ITrendingVideoFeed
{
    Task<VideoInfo[]> GetTrendingVideos(string targetVideo, int takeNext);
}

public class TrendingVideoFeed(
    UserInfo currentUser,
    IUserPermissionService userPermissionService,
    IGeoClusterProvider geoClusterProvider,
    ILogger<TrendingVideoFeed> log,
    IVideoLoader videoLoader,
    VideoServerOptions options,
    ITrendingVideoFeedRepository repo
) : ITrendingVideoFeed
{
    public async Task<VideoInfo[]> GetTrendingVideos(string targetVideo, int takeNext)
    {
        await userPermissionService.EnsureCurrentUserActive();

        var geoCluster = await geoClusterProvider.DetectGeoClusterForGroup(currentUser);

        log.LogInformation("TrendingVideo: target={Target}, next={TakeNext} geoCluster={GId}", targetVideo, takeNext, geoCluster?.Id);

        return await videoLoader.LoadVideoPage(
                   FetchVideoInfoFrom.ReadDb,
                   (target, next, _) => repo.GetTrendingVideo(
                       geoCluster,
                       currentUser,
                       options.TrendingVideoListLength,
                       target,
                       next
                   ),
                   Sorting.Asc,
                   targetVideo,
                   takeNext
               );
    }
}