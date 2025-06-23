using System.Threading.Tasks;
using AuthServer.Permissions.Services;
using AuthServerShared;
using Frever.Video.Contract;
using Frever.Video.Core.Features.Hashtags.DataAccess;
using Frever.Videos.Shared.GeoClusters;
using Microsoft.Extensions.Logging;

namespace Frever.Video.Core.Features.Hashtags.Feed;

public interface IHashtagVideoFeed
{
    Task<VideoInfo[]> GetHashtagVideoFeed(long hashtagId, string targetVideo, int takeNext);
}

public class HashtagVideoFeed(
    UserInfo currentUser,
    IUserPermissionService userPermissionService,
    IGeoClusterProvider geoClusterProvider,
    ILogger<HashtagVideoFeed> log,
    IVideoLoader videoLoader,
    IHashtagRepository repo
) : IHashtagVideoFeed
{
    public async Task<VideoInfo[]> GetHashtagVideoFeed(long hashtagId, string targetVideo, int takeNext)
    {
        await userPermissionService.EnsureCurrentUserActive();

        var geoCluster = await geoClusterProvider.DetectGeoClusterForGroup(currentUser);

        log.LogInformation(
            "GetHashtagVideosFeed: hashtagId={HashtagId}, target={Target}, next={TakeNext}, geoCluster={GId}",
            hashtagId,
            targetVideo,
            takeNext,
            geoCluster?.Id
        );

        return await videoLoader.LoadVideoPage(
                   FetchVideoInfoFrom.ReadDb,
                   (target, next, _) => repo.GetVideoByHashtagId(
                       geoCluster,
                       currentUser,
                       hashtagId,
                       target,
                       next
                   ),
                   Sorting.Desc,
                   targetVideo,
                   takeNext
               );
    }
}