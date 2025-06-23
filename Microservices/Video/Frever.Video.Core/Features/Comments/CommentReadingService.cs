using System;
using System.Linq;
using System.Threading.Tasks;
using AuthServer.Permissions.Services;
using AuthServerShared;
using Frever.Client.Shared.Social.Services;
using Frever.Shared.MainDb.Entities;
using Frever.Video.Contract;
using Frever.Video.Core.Features.Comments.DataAccess;
using Frever.Video.Core.Features.Shared;

namespace Frever.Video.Core.Features.Comments;

public class CommentReadingService(
    IUserPermissionService userPermissionService,
    ISocialSharedService socialSharedService,
    UserInfo currentUser,
    ICommentReadingRepository repo,
    IUserCommentInfoProvider commentInfoProvider,
    IOneVideoAccessor oneVideoAccessor
) : ICommentReadingService
{
    public async Task<UserCommentInfo> GetCommentById(long videoId, long commentId)
    {
        await userPermissionService.EnsureCurrentUserActive();

        if (!await oneVideoAccessor.IsVideoAccessibleTo(FetchVideoInfoFrom.WriteDb, currentUser, videoId))
            return null;

        var blockedGroups = await socialSharedService.GetBlocked(currentUser);

        var comments = await MakeUserCommentsInfo(videoId, q => q.Where(c => c.Id == commentId), blockedGroups);

        return comments.FirstOrDefault();
    }

    public async Task<IQueryable<long>> GetWhoCommented(long videoId)
    {
        await userPermissionService.EnsureCurrentUserActive();

        if (!await oneVideoAccessor.IsVideoAccessibleTo(FetchVideoInfoFrom.WriteDb, currentUser, videoId))
            return null;

        var blocked = await socialSharedService.GetBlocked(currentUser);

        return repo.GetVideoComments(videoId).Where(c => !blocked.Contains(c.GroupId)).GroupBy(c => c.GroupId).Select(g => g.Key);
    }

    public async Task<UserCommentInfo[]> GetRootComments(long videoId, string key = null, int takeOlder = 20, int takeNewer = 20)
    {
        await userPermissionService.EnsureCurrentUserActive();

        var blockedGroups = await socialSharedService.GetBlocked(currentUser);

        takeNewer = Math.Clamp(takeNewer, 0, 500);
        takeOlder = Math.Clamp(takeOlder, 0, 500);

        var comments = repo.GetRootComments(videoId, key, takeOlder, takeNewer);

        return await commentInfoProvider.MakeUserCommentsInfo(
                   comments,
                   repo.GetVideoCommentLikes(videoId, currentUser),
                   repo.GetCommentGroupInfo(videoId),
                   blockedGroups
               );
    }

    public async Task<UserCommentInfo[]> GetThreadComments(
        long videoId,
        string rootCommentKey,
        string key = null,
        int takeOlder = 20,
        int takeNewer = 20
    )
    {
        if (string.IsNullOrWhiteSpace(rootCommentKey))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(rootCommentKey));

        await userPermissionService.EnsureCurrentUserActive();

        var blockedGroups = await socialSharedService.GetBlocked(currentUser);

        takeNewer = Math.Clamp(takeNewer, 0, 500);
        takeOlder = Math.Clamp(takeOlder, 0, 500);

        var comments = repo.GetThreadCommentRange(
                                videoId,
                                rootCommentKey,
                                key,
                                takeOlder,
                                takeNewer,
                                blockedGroups
                            )
                           .OrderBy(c => c.Thread);
        return await commentInfoProvider.MakeUserCommentsInfo(
                   comments,
                   repo.GetVideoCommentLikes(videoId, currentUser),
                   repo.GetCommentGroupInfo(videoId),
                   blockedGroups
               );
    }

    public async Task<UserCommentInfo[]> GetPinnedComments(long videoId)
    {
        await userPermissionService.EnsureCurrentUserActive();

        var blockedGroups = await socialSharedService.GetBlocked(currentUser);
        var comments = repo.GetVideoComments(videoId).Where(c => c.IsPinned).OrderByDescending(c => c.Id);

        return await commentInfoProvider.MakeUserCommentsInfo(
                   comments,
                   repo.GetVideoCommentLikes(videoId, currentUser),
                   repo.GetCommentGroupInfo(videoId),
                   blockedGroups
               );
    }

    private async Task<UserCommentInfo[]> MakeUserCommentsInfo(
        long videoId,
        Func<IQueryable<Comment>, IQueryable<Comment>> buildQuery,
        long[] blockedGroups
    )
    {
        ArgumentNullException.ThrowIfNull(buildQuery);
        ArgumentNullException.ThrowIfNull(blockedGroups);

        var comments = repo.GetVideoComments(videoId);

        var query = buildQuery(comments);

        return await commentInfoProvider.MakeUserCommentsInfo(
                   query,
                   repo.GetVideoCommentLikes(videoId, currentUser),
                   repo.GetCommentGroupInfo(videoId),
                   blockedGroups
               );
    }
}