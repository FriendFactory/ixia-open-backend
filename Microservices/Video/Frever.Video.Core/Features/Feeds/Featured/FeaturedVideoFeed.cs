using System.Threading.Tasks;
using AuthServer.Permissions.Services;
using AuthServerShared;
using Frever.Video.Contract;
using Frever.Videos.Shared.GeoClusters;
using Microsoft.Extensions.Logging;

namespace Frever.Video.Core.Features.Feeds.Featured;

public interface IFeaturedVideoFeed
{
    Task<VideoInfo[]> FeaturedVideos(string targetVideo, int takeNext, int takePrevious);
}

public class FeaturedVideoFeed(
    UserInfo currentUser,
    IUserPermissionService userPermissionService,
    IGeoClusterProvider geoClusterProvider,
    ILogger<FeaturedVideoFeed> log,
    IVideoLoader videoLoader,
    IFeaturedVideoFeedRepository repo
) : IFeaturedVideoFeed
{
    public async Task<VideoInfo[]> FeaturedVideos(string targetVideo, int takeNext, int takePrevious)
    {
        await userPermissionService.EnsureCurrentUserActive();

        var geoCluster = await geoClusterProvider.DetectGeoClusterForGroup(currentUser);

        log.LogInformation(
            "FeaturedVideos: target={Target}, next={TakeNext}, previous={TakePrevious} geoCluster={GId}",
            targetVideo,
            takeNext,
            takePrevious,
            geoCluster?.Id
        );

        return await videoLoader.LoadVideoPage(
                   FetchVideoInfoFrom.ReadDb,
                   (target, next, prev) => repo.GetFeaturedVideos(
                       geoCluster,
                       currentUser,
                       target,
                       next,
                       prev
                   ),
                   Sorting.Desc,
                   targetVideo,
                   takeNext,
                   takePrevious
               );
    }
}