using System;
using System.Linq;
using System.Threading.Tasks;
using Frever.Client.Shared.Social.Services;
using Frever.Shared.MainDb.Entities;
using Microsoft.EntityFrameworkCore;
using NotificationService.DataAccess;
using NotificationService.Shared;

namespace NotificationService.Core;

public interface IVideoServerService
{
    Task<VideoDetails> GetVideoDetails(long id, long currentGroupId, long[] blockedGroups);

    Task<long> GetVideoGroupId(long id);

    Task<CommentInfo> GetCommentInfo(long videoId, long commentId, long[] blockedGroups);
}

public class VideoServerService(INotificationRepository repo, ISocialSharedService socialService) : IVideoServerService
{
    private readonly ISocialSharedService _socialService = socialService ?? throw new ArgumentNullException(nameof(socialService));

    public async Task<VideoDetails> GetVideoDetails(long id, long currentGroupId, long[] blockedGroups)
    {
        var video = await repo.GetVideoUnsafe(id).Include(v => v.VideoMentions).Include(v => v.VideoGroupTags).FirstOrDefaultAsync();
        if (video == null)
            return null;

        if (blockedGroups != null && blockedGroups.Contains(video.GroupId))
            return null;

        if (!await IsVideoAccessible(video, currentGroupId))
            return null;

        return new VideoDetails
               {
                   Id = video.Id,
                   GroupId = video.GroupId,
                   RemixedFromVideoId = video.RemixedFromVideoId,
                   Access = Convert.ToInt32(video.Access),
                   Mentions = video.VideoMentions.Select(e => e.GroupId).ToArray(),
                   CharacterTags = video.VideoGroupTags.Where(e => e.IsCharacterTag).Select(e => e.GroupId).ToArray(),
                   NonCharacterTags = video.VideoGroupTags.Where(e => !e.IsCharacterTag).Select(e => e.GroupId).ToArray()
               };
    }

    public Task<long> GetVideoGroupId(long id)
    {
        return repo.GetVideoUnsafe(id).Select(e => e.GroupId).FirstOrDefaultAsync();
    }

    public async Task<CommentInfo> GetCommentInfo(long videoId, long commentId, long[] blockedGroups)
    {
        var comment = await repo.GetComment(commentId, videoId).SingleOrDefaultAsync();

        if (comment == null || comment.IsDeleted || blockedGroups.Contains(comment.GroupId))
            return null;

        var group = await repo.GetGroup(comment.GroupId).SingleOrDefaultAsync();

        var result = new CommentInfo
                     {
                         GroupId = comment.GroupId,
                         GroupNickname = group?.NickName,
                         Id = comment.Id,
                         Key = comment.Thread,
                         Text = comment.Text,
                         Time = comment.Time,
                         VideoId = comment.VideoId
                     };

        if (comment.ReplyToCommentId == null)
            return result;

        var replyComment = await repo.GetComment(comment.ReplyToCommentId.Value, comment.VideoId).SingleOrDefaultAsync();

        if (replyComment == null || replyComment.IsDeleted || blockedGroups.Contains(replyComment.GroupId))
            return result;

        var replyGroup = await repo.GetGroup(replyComment.GroupId).SingleOrDefaultAsync();

        result.ReplyToComment = new CommentGroupInfo
                                {
                                    CommentId = replyComment.Id, GroupId = replyComment.GroupId, GroupNickname = replyGroup?.NickName
                                };

        return result;
    }

    private async Task<bool> IsVideoAccessible(Video video, long currentGroupId)
    {
        if (video.GroupId == currentGroupId)
            return true;

        return video.Access switch
               {
                   VideoAccess.Private         => false,
                   VideoAccess.Public          => true,
                   VideoAccess.ForFollowers    => await _socialService.IsFollowed(currentGroupId, video.GroupId),
                   VideoAccess.ForFriends      => await _socialService.IsFriend(currentGroupId, video.GroupId),
                   VideoAccess.ForTaggedGroups => video.VideoGroupTags.Any(e => e.GroupId == currentGroupId),
                   _                           => throw new ArgumentOutOfRangeException(nameof(video.Access))
               };
    }
}