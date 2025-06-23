using System;
using System.Linq;
using System.Threading.Tasks;
using Frever.Shared.MainDb.Entities;
using NotificationService.DataAccess;
using NotificationService.Shared;
using NotificationService.Shared.Notifications;

namespace NotificationService.Core;

public interface INotificationMapper
{
    Task<NotificationBase> Map(long currentGroupId, NotificationView notification, long[] blockedGroups);
}

public class NotificationMapper(IMainServerService mainService, IVideoServerService videoService) : INotificationMapper
{
    public async Task<NotificationBase> Map(long currentGroupId, NotificationView src, long[] blockedGroups)
    {
        ArgumentNullException.ThrowIfNull(src);

        switch (src.Notification.Type)
        {
            case NotificationType.NewFollower:
                if (src.DataGroup == null)
                    throw new InvalidOperationException("Missing group info for notification");

                return new NewFollowerNotification
                       {
                           Id = src.Notification.Id,
                           Expires = src.Notification.Expires,
                           NotificationType = src.Notification.Type,
                           Timestamp = src.Notification.TimeStamp,
                           HasRead = src.HasRead,
                           FollowedBy = await FromGroup(currentGroupId, src.DataGroup)
                       };

            case NotificationType.NewLikeOnVideo:
                if (src.DataGroup == null)
                    throw new InvalidOperationException("Missing group info for notification");
                if (src.Notification.DataVideoId == null)
                    throw new InvalidOperationException("Missing video id for notification");

                return new NewLikeOnVideoNotification
                       {
                           Id = src.Notification.Id,
                           Expires = src.Notification.Expires,
                           NotificationType = src.Notification.Type,
                           Timestamp = src.Notification.TimeStamp,
                           HasRead = src.HasRead,
                           LikedBy = await FromGroup(currentGroupId, src.DataGroup),
                           LikedVideo = await FromVideoId(currentGroupId, src.Notification.DataVideoId.Value, blockedGroups)
                       };

            case NotificationType.YouTaggedOnVideo:
            case NotificationType.NonCharacterTagOnVideo:
                if (src.DataGroup == null)
                    throw new InvalidOperationException("Missing group info for notification");
                if (src.Notification.DataVideoId == null)
                    throw new InvalidOperationException("Missing video id for notification");

                return new YouTaggedOnVideoNotification
                       {
                           Id = src.Notification.Id,
                           Expires = src.Notification.Expires,
                           NotificationType = src.Notification.Type,
                           Timestamp = src.Notification.TimeStamp,
                           HasRead = src.HasRead,
                           TaggedBy = await FromGroup(currentGroupId, src.DataGroup),
                           TaggedOnVideo = await FromVideoId(currentGroupId, src.Notification.DataVideoId.Value, blockedGroups)
                       };

            case NotificationType.YourVideoRemixed:
                if (src.DataGroup == null)
                    throw new InvalidOperationException("Missing group info for notification");
                if (src.Notification.DataVideoId == null)
                    throw new InvalidOperationException("Missing video id for notification");

                var (video, remixedFromVideo) = await VideoAndRemixFromVideoId(
                                                    currentGroupId,
                                                    src.Notification.DataVideoId.Value,
                                                    blockedGroups
                                                );

                return new YourVideoRemixedNotification
                       {
                           Id = src.Notification.Id,
                           Expires = src.Notification.Expires,
                           NotificationType = src.Notification.Type,
                           Timestamp = src.Notification.TimeStamp,
                           HasRead = src.HasRead,
                           RemixedBy = await FromGroup(currentGroupId, src.DataGroup),
                           Remix = video,
                           RemixedFromVideo = remixedFromVideo
                       };
            case NotificationType.NewFriendVideo:
                if (src.DataGroup == null)
                    throw new InvalidOperationException("Missing group info for notification");
                if (src.Notification.DataVideoId == null)
                    throw new InvalidOperationException("Missing video id for notification");

                return new NewFriendVideoNotification
                       {
                           Id = src.Notification.Id,
                           Expires = src.Notification.Expires,
                           NotificationType = src.Notification.Type,
                           Timestamp = src.Notification.TimeStamp,
                           HasRead = src.HasRead,
                           PostedBy = await FromGroup(currentGroupId, src.DataGroup),
                           NewVideo = await FromVideoId(currentGroupId, src.Notification.DataVideoId.Value, blockedGroups)
                       };
            case NotificationType.NewMentionOnVideo:
                if (src.DataGroup == null)
                    throw new InvalidOperationException("Missing group info for notification");
                if (src.Notification.DataVideoId == null)
                    throw new InvalidOperationException("Missing video id for notification");

                return new NewMentionOnVideo
                       {
                           Id = src.Notification.Id,
                           Expires = src.Notification.Expires,
                           NotificationType = src.Notification.Type,
                           Timestamp = src.Notification.TimeStamp,
                           HasRead = src.HasRead,
                           MentionedVideo = await FromVideoId(currentGroupId, src.Notification.DataVideoId.Value, blockedGroups),
                           MentionedBy = await FromGroup(currentGroupId, src.DataGroup)
                       };
            case NotificationType.NewCommentOnVideo:
                if (src.DataGroup == null)
                    throw new InvalidOperationException("Missing group info for notification");
                if (src.Notification.DataVideoId == null)
                    throw new InvalidOperationException("Missing video id for notification");

                return new NewCommentOnVideoNotification
                       {
                           Id = src.Notification.Id,
                           Expires = src.Notification.Expires,
                           NotificationType = src.Notification.Type,
                           Timestamp = src.Notification.TimeStamp,
                           HasRead = src.HasRead,
                           CommentedVideo = await FromVideoId(currentGroupId, src.Notification.DataVideoId.Value, blockedGroups),
                           CommentedBy = await FromGroup(currentGroupId, src.DataGroup),
                           Comment = await FromComment(
                                         currentGroupId,
                                         src.Notification.DataVideoId.Value,
                                         src.Notification.DataRefId.Value,
                                         blockedGroups
                                     )
                       };
            case NotificationType.NewMentionInCommentOnVideo:
                if (src.DataGroup == null)
                    throw new InvalidOperationException("Missing group info for notification");
                if (src.Notification.DataVideoId == null)
                    throw new InvalidOperationException("Missing video id for notification");

                return new NewMentionInCommentOnVideo
                       {
                           Id = src.Notification.Id,
                           Expires = src.Notification.Expires,
                           NotificationType = src.Notification.Type,
                           Timestamp = src.Notification.TimeStamp,
                           HasRead = src.HasRead,
                           CommentedVideo = await FromVideoId(currentGroupId, src.Notification.DataVideoId.Value, blockedGroups),
                           CommentedBy = await FromGroup(currentGroupId, src.DataGroup),
                           Comment = await FromComment(
                                         currentGroupId,
                                         src.Notification.DataVideoId.Value,
                                         src.Notification.DataRefId.Value,
                                         blockedGroups
                                     )
                       };
            case NotificationType.NewCommentOnVideoYouHaveCommented:
                if (src.DataGroup == null)
                    throw new InvalidOperationException("Missing group info for notification");
                if (src.Notification.DataVideoId == null)
                    throw new InvalidOperationException("Missing video id for notification");

                return new NewCommentOnVideoYouHaveCommentedNotification
                       {
                           Id = src.Notification.Id,
                           Expires = src.Notification.Expires,
                           NotificationType = src.Notification.Type,
                           Timestamp = src.Notification.TimeStamp,
                           HasRead = src.HasRead,
                           CommentedBy = await FromGroup(currentGroupId, src.DataGroup),
                           CommentedVideo = await FromVideoId(currentGroupId, src.Notification.DataVideoId.Value, blockedGroups),
                           Comment = await FromComment(
                                         currentGroupId,
                                         src.Notification.DataVideoId.Value,
                                         src.Notification.DataRefId.Value,
                                         blockedGroups
                                     )
                       };

            case NotificationType.YourVideoConverted:
                if (src.DataGroup == null)
                    throw new InvalidOperationException("Missing group info for notification");
                if (src.Notification.DataVideoId == null)
                    throw new InvalidOperationException("Missing video id for notification");

                return new YourVideoConversionCompletedNotification
                       {
                           Id = src.Notification.Id,
                           Expires = src.Notification.Expires,
                           NotificationType = src.Notification.Type,
                           Timestamp = src.Notification.TimeStamp,
                           HasRead = src.HasRead,
                           ConvertedVideo = await FromVideoId(currentGroupId, src.Notification.DataVideoId.Value, blockedGroups),
                           Owner = await FromGroup(currentGroupId, src.DataGroup)
                       };

            case NotificationType.VideoDeleted:
                if (src.DataGroup == null)
                    throw new InvalidOperationException("Missing group info for notification");
                if (src.Notification.DataVideoId == null)
                    throw new InvalidOperationException("Missing video id for notification");

                return new VideoDeletedNotification
                       {
                           Id = src.Notification.Id,
                           Expires = src.Notification.Expires,
                           NotificationType = src.Notification.Type,
                           Timestamp = src.Notification.TimeStamp,
                           HasRead = src.HasRead
                       };

            case NotificationType.AiContentGenerated:
                if (src.DataGroup == null)
                    throw new InvalidOperationException("Missing group info for notification");
                if (src.Notification.DataAiContentId == null)
                    throw new InvalidOperationException("Missing content id for notification");

                return new NewAiContentGeneratedNotification
                       {
                           Id = src.Notification.Id,
                           Expires = src.Notification.Expires,
                           NotificationType = src.Notification.Type,
                           Timestamp = src.Notification.TimeStamp,
                           HasRead = src.HasRead,
                           AiContentInfo = await mainService.GetContentInfo(src.Notification.DataAiContentId.Value, currentGroupId)
                       };

            default:
                return null;
        }
    }

    private async Task<GroupInfo> FromGroup(long currentGroupId, Group group)
    {
        ArgumentNullException.ThrowIfNull(group);

        return new GroupInfo
               {
                   Id = group.Id, Nickname = group.NickName, AreFriends = await mainService.IsMyFriend(currentGroupId, group.Id)
               };
    }

    private async Task<Shared.CommentInfo> FromComment(long currentGroupId, long videoId, long commentId, long[] blockedGroups)
    {
        ArgumentNullException.ThrowIfNull(blockedGroups);

        var comment = await videoService.GetCommentInfo(videoId, commentId, blockedGroups);
        if (comment == null)
            return null;

        return new Shared.CommentInfo
               {
                   Id = comment.Id,
                   Key = comment.Key,
                   CommentedBy =
                       new GroupInfo
                       {
                           AreFriends = await mainService.IsMyFriend(currentGroupId, comment.GroupId),
                           Id = comment.GroupId,
                           Nickname = comment.GroupNickname
                       },
                   ReplyTo = comment.ReplyToComment == null
                                 ? null
                                 : new Shared.CommentInfo
                                   {
                                       Id = comment.ReplyToComment.CommentId,
                                       Key = GetParentCommentKey(comment.Key),
                                       CommentedBy = new GroupInfo
                                                     {
                                                         AreFriends = await mainService.IsMyFriend(
                                                                          currentGroupId,
                                                                          comment.ReplyToComment.GroupId
                                                                      ),
                                                         Id = comment.ReplyToComment.GroupId,
                                                         Nickname = comment.ReplyToComment.GroupNickname
                                                     }
                                   }
               };
    }

    private async Task<VideoInfo> FromVideoId(long currentGroupId, long videoId, long[] blockedGroups)
    {
        var video = await videoService.GetVideoDetails(videoId, currentGroupId, blockedGroups);

        return video == null ? null : new VideoInfo {Id = videoId};
    }

    private async Task<(VideoInfo video, VideoInfo remixedFromVideo)> VideoAndRemixFromVideoId(
        long currentGroupId,
        long videoId,
        long[] blockedGroups
    )
    {
        var video = await videoService.GetVideoDetails(videoId, currentGroupId, blockedGroups);

        if (video == null)
            return (null, null);

        var remix = video.RemixedFromVideoId == null
                        ? null
                        : await videoService.GetVideoDetails(video.RemixedFromVideoId.Value, currentGroupId, blockedGroups);

        return (video, remix);
    }

    private static string GetParentCommentKey(string commentKey)
    {
        const char separator = '.';

        if (string.IsNullOrWhiteSpace(commentKey) || !commentKey.Contains(separator))
            return null;

        return string.Join(separator, commentKey.Split(separator).Reverse().Skip(1).Reverse());
    }
}