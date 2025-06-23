using System.Threading.Tasks;
using AuthServer.Permissions.Services;
using AuthServerShared;
using Frever.Video.Contract;
using Frever.Video.Core.Features.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Frever.Video.Core.Features.Feeds.Remixes;

public interface IRemixesOfVideoFeed
{
    Task<VideoInfo[]> GetRemixesOfVideo(long videoId, string targetVideo, int takeNext, int takePrevious);
}

public class RemixesOfVideoFeed(
    UserInfo currentUser,
    IUserPermissionService userPermissionService,
    ILogger<RemixesOfVideoFeed> log,
    IVideoLoader videoLoader,
    IOneVideoAccessor oneVideoAccessor,
    IRemixesOfVideoRepository repo
) : IRemixesOfVideoFeed
{
    public async Task<VideoInfo[]> GetRemixesOfVideo(long videoId, string targetVideo, int takeNext, int takePrevious)
    {
        await userPermissionService.EnsureCurrentUserActive();

        log.LogInformation(
            "GetRemixesOfVideo: videoId={VideoId} target={Target}, next={TakeNext}, previous={TakePrevious}",
            videoId,
            targetVideo,
            takeNext,
            takePrevious
        );

        if (!await oneVideoAccessor.IsVideoAccessibleTo(FetchVideoInfoFrom.ReadDb, currentUser, videoId))
            return [];

        var video = await repo.GetVideo(videoId).SingleOrDefaultAsync();
        if (video is null)
            return [];

        var origin = video.RemixedFromVideoId ?? video.Id;

        return await videoLoader.LoadVideoPage(
                   FetchVideoInfoFrom.ReadDb,
                   (target, next, prev) => repo.GetRemixesOfVideo(
                       origin,
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