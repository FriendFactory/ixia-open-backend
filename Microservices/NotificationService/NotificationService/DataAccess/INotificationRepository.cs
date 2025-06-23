using System;
using System.Linq;
using System.Threading.Tasks;
using Common.Models.Files;
using Frever.ClientService.Contract.Ai;
using Frever.Shared.MainDb.Entities;

namespace NotificationService.DataAccess;

public interface INotificationRepository
{
    IQueryable<NotificationView> AllGroupNotifications(long groupId);

    Task<(Notification notification, long[] sentToGroupIds)> AddNotification(
        Notification notification,
        long[] targetGroupIds,
        bool allowRepeatableNotifications,
        TimeSpan? notMoreOftenThen = null
    );

    Task MarkNotificationsAsRead(long groupId, long[] ids);

    Task<bool> AreFriends(long groupId1, long groupId2);

    Task<bool> HaveFollower(long groupId, long followerId);

    IQueryable<long> GetFriendGroupIds(long groupId);

    IQueryable<Video> GetVideoUnsafe(long id);

    IQueryable<Comment> GetComment(long id, long videoId);

    IQueryable<Group> GetGroup(long id);

    Task<AiGeneratedContentInfo> GetAiContent(long contentId, long groupId);
}

public class NotificationView
{
    public long Id { get; set; }

    public Notification Notification { get; set; }

    public Group DataGroup { get; set; }

    public bool HasRead { get; set; }
}

public class AiGeneratedContentInfo : IFileMetadataOwner
{
    public required long ContentId { get; set; }
    public required AiGeneratedContentType Type { get; set; }
    public required long? RemixedFromAiGeneratedContentId { get; set; }
    public required DateTime CreatedAt { get; set; }
    public required long Id { get; set; }
    public required FileMetadata[] Files { get; set; }
}