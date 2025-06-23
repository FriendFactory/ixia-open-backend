using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Frever.Shared.MainDb.Entities;
using Frever.Video.Core.Utils;

namespace Frever.Video.Core.Features.Comments;

public interface IUserCommentInfoProvider
{
    Task<UserCommentInfo[]> MakeUserCommentsInfo(
        IQueryable<Comment> comments,
        IQueryable<CommentLike> likes,
        IQueryable<CommentGroupInfo> commentGroupInfo,
        long[] blockedGroups
    );
}

public class UserCommentInfoProvider : IUserCommentInfoProvider
{
    public async Task<UserCommentInfo[]> MakeUserCommentsInfo(
        IQueryable<Comment> comments,
        IQueryable<CommentLike> likes,
        IQueryable<CommentGroupInfo> commentGroupInfo,
        long[] blockedGroups
    )
    {
        ArgumentNullException.ThrowIfNull(comments);

        var result = await comments.Where(c => !blockedGroups.Contains(c.GroupId))
                                    // Replies
                                   .GroupJoin(
                                        commentGroupInfo.Where(c => !blockedGroups.Contains(c.GroupId)),
                                        a => a.ReplyToCommentId,
                                        g => g.CommentId,
                                        (a, g) => new {Comment = a, Reply = g}
                                    )
                                   .SelectMany(
                                        a => a.Reply.DefaultIfEmpty(),
                                        (a, g) => new UserCommentInfo
                                                  {
                                                      Id = a.Comment.Id,
                                                      Text = a.Comment.Text,
                                                      Time = a.Comment.Time,
                                                      GroupId = a.Comment.GroupId,
                                                      VideoId = a.Comment.VideoId,
                                                      GroupNickname = a.Comment.Group.NickName,
                                                      GroupCreatorScoreBadge = a.Comment.Group.CreatorScoreBadge,
                                                      Mentions = a.Comment.Mentions ?? new List<Mention>(),
                                                      ReplyCount = a.Comment.ReplyCount,
                                                      Key = a.Comment.Thread,
                                                      ReplyToComment = g,
                                                      LikeCount = a.Comment.LikeCount,
                                                      IsLikedByCurrentUser = likes.Any(cl => cl.CommentId == a.Comment.Id),
                                                      IsPinned = a.Comment.IsPinned
                                                  }
                                    )
                                   .ToArrayAsyncSafe();

        return result;
    }
}