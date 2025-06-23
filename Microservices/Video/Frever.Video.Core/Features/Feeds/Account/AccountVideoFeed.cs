using System.Threading.Tasks;
using AuthServer.Permissions.Services;
using AuthServerShared;
using Frever.Client.Shared.Social.Services;
using Frever.Video.Contract;
using Microsoft.Extensions.Logging;

namespace Frever.Video.Core.Features.Feeds.Account;

public interface IAccountVideoFeed
{
    Task<VideoInfo[]> GetMyFriendsVideos(string targetVideo, int takeNext, int takePrevious);
    Task<VideoInfo[]> MyFollowingFeedVideo(string targetVideo, int takeNext, int takePrevious);
    Task<VideoInfo[]> GetMyVideos(string targetVideo, int takeNext, int takePrevious);
    Task<VideoInfo[]> GetGroupVideos(long groupId, string targetVideo, int takeNext, int takePrevious);
}

public class AccountVideoFeed(
    UserInfo currentUser,
    IUserPermissionService userPermissionService,
    ILogger<AccountVideoFeed> log,
    IVideoLoader videoLoader,
    IAccountVideoFeedRepository repo,
    ISocialSharedService socialSharedService
) : IAccountVideoFeed
{
    public async Task<VideoInfo[]> GetMyFriendsVideos(string targetVideo, int takeNext, int takePrevious)
    {
        await userPermissionService.EnsureCurrentUserActive();

        log.LogInformation(
            "GetMyFriendsVideos: target={Target}, next={TakeNext}, previous={TakePrevious}",
            targetVideo,
            takeNext,
            takePrevious
        );

        return await videoLoader.LoadVideoPage(
                   FetchVideoInfoFrom.ReadDb,
                   (target, next, prev) => repo.GetFriendVideo(currentUser, target, next, prev),
                   Sorting.Desc,
                   targetVideo,
                   takeNext,
                   takePrevious
               );
    }

    public async Task<VideoInfo[]> MyFollowingFeedVideo(string targetVideo, int takeNext, int takePrevious)
    {
        await userPermissionService.EnsureCurrentUserActive();

        log.LogInformation(
            "MyFollowingFeedVideo: target={Target}, next={TakeNext}, previous={TakePrevious}",
            targetVideo,
            takeNext,
            takePrevious
        );

        return await videoLoader.LoadVideoPage(
                   FetchVideoInfoFrom.ReadDb,
                   (target, next, prev) => repo.GetFollowingVideo(currentUser, target, next, prev),
                   Sorting.Desc,
                   targetVideo,
                   takeNext,
                   takePrevious
               );
    }

    public async Task<VideoInfo[]> GetMyVideos(string targetVideo, int takeNext, int takePrevious)
    {
        await userPermissionService.EnsureCurrentUserActive();

        log.LogInformation("GetMyVideos: target={Target}, next={TakeNext}, previous={TakePrevious}", targetVideo, takeNext, takePrevious);

        return await videoLoader.LoadVideoPage(
                   FetchVideoInfoFrom.ReadDb,
                   (target, next, prev) => repo.GetGroupAvailableVideo(
                       currentUser,
                       currentUser,
                       target,
                       next,
                       prev,
                       false,
                       true
                   ),
                   Sorting.Desc,
                   targetVideo,
                   takeNext,
                   takePrevious
               );
    }

    public async Task<VideoInfo[]> GetGroupVideos(long groupId, string targetVideo, int takeNext, int takePrevious)
    {
        await userPermissionService.EnsureCurrentUserActive();

        log.LogInformation(
            "GetGroupVideos: groupId={GroupId} target={Target}, next={TakeNext}, previous={TakePrevious}",
            groupId,
            targetVideo,
            takeNext,
            takePrevious
        );

        if (groupId != currentUser && await socialSharedService.IsBlocked(groupId, currentUser))
            return [];

        return await videoLoader.LoadVideoPage(
                   FetchVideoInfoFrom.ReadDb,
                   (target, next, prev) => repo.GetGroupAvailableVideo(
                       groupId,
                       currentUser,
                       target,
                       next,
                       prev,
                       false,
                       true
                   ),
                   Sorting.Desc,
                   targetVideo,
                   takeNext,
                   takePrevious
               );
    }
}