using System.Threading.Tasks;
using AuthServer.Permissions.Services;
using AuthServerShared;
using Frever.Client.Shared.Social.Services;
using Frever.Video.Contract;
using Microsoft.Extensions.Logging;

namespace Frever.Video.Core.Features.Feeds.TaggedIn;

public interface ITaggedInVideoFeed
{
    Task<VideoInfo[]> VideoUserTaggedIn(long groupId, string targetVideo, int takeNext, int takePrevious);
}

public class TaggedInVideoFeed(
    UserInfo currentUser,
    IUserPermissionService userPermissionService,
    ISocialSharedService socialSharedService,
    ILogger<TaggedInVideoFeed> log,
    IVideoLoader videoLoader,
    ITaggedInVideoRepository repo
) : ITaggedInVideoFeed
{
    public async Task<VideoInfo[]> VideoUserTaggedIn(long groupId, string targetVideo, int takeNext, int takePrevious)
    {
        await userPermissionService.EnsureCurrentUserActive();

        log.LogInformation(
            "VideoUserTaggedIn: groupId={GroupId} target={Target}, next={TakeNext}, previous={TakePrevious}",
            groupId,
            targetVideo,
            takeNext,
            takePrevious
        );

        if (groupId != currentUser && await socialSharedService.IsBlocked(groupId, currentUser))
            return [];

        return await videoLoader.LoadVideoPage(
                   FetchVideoInfoFrom.ReadDb,
                   (target, next, prev) => repo.GetVideoUserTaggedIn(
                       groupId,
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