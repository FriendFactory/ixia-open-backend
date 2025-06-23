using System;
using System.Linq;
using System.Threading.Tasks;
using Frever.ClientService.Contract.Ai;
using Frever.Shared.MainDb;
using Frever.Shared.MainDb.Entities;
using Microsoft.EntityFrameworkCore;

namespace NotificationService.DataAccess;

internal sealed class EntityFrameworkNotificationRepository(IWriteDb db) : INotificationRepository
{
    private const string BasicCommentsSql = """
                                            select
                                                c."Id",
                                                c."VideoId",
                                                c."Time",
                                                c."GroupId",
                                                c."Text",
                                                c."IsDeleted",
                                                c."Mentions",
                                                c."Thread"::text "Thread",
                                                c."ReplyToCommentId",
                                                c."ReplyCount",
                                                c."LikeCount",
                                                c."IsPinned"
                                            from "Comments" c
                                            """;

    public IQueryable<NotificationView> AllGroupNotifications(long groupId)
    {
        return AllNotifications()
           .Where(n => db.NotificationAndGroup.Any(nu => nu.GroupId == groupId && nu.NotificationId == n.Notification.Id));
    }

    public async Task<(Notification notification, long[] sentToGroupIds)> AddNotification(
        Notification notification,
        long[] targetGroupIds,
        bool allowRepeatableNotifications,
        TimeSpan? notMoreOftenThen
    )
    {
        var groupsQuery = db.Group.Where(g => targetGroupIds.Contains(g.Id));
        if (!allowRepeatableNotifications || notMoreOftenThen != null)
        {
            var older = DateTime.UtcNow - (notMoreOftenThen ?? TimeSpan.Zero);
            groupsQuery = groupsQuery.Where(
                g => !db.Notification.Where(n => notMoreOftenThen == null || n.TimeStamp >= older)
                         .Where(n => db.NotificationAndGroup.Any(ng => ng.NotificationId == n.Id && ng.GroupId == g.Id))
                         .Any(
                              n => n.Type == notification.Type &&
                                   (notification.DataVideoId == null || notification.DataVideoId == n.DataVideoId) &&
                                   (notification.DataGroupId == null || notification.DataGroupId == n.DataGroupId) &&
                                   (notification.DataAiContentId == null || notification.DataAiContentId == n.DataAiContentId) &&
                                   (notification.DataRefId == null || notification.DataRefId == n.DataRefId)
                          )
            );
        }

        var groups = await groupsQuery.ToArrayAsync();

        if (groups.Length == 0)
            return (null, null);

        db.Notification.Add(notification);
        await db.SaveChangesAsync();

        foreach (var group in groups)
            db.NotificationAndGroup.Add(new NotificationAndGroup {NotificationId = notification.Id, GroupId = group.Id});

        await db.SaveChangesAsync();

        return (notification, groups.Select(g => g.Id).ToArray());
    }

    public async Task MarkNotificationsAsRead(long groupId, long[] ids)
    {
        await using var transaction = await db.BeginTransaction();

        var ng = await db.NotificationAndGroup.Where(nu => nu.GroupId == groupId && ids.Contains(nu.NotificationId) && !nu.HasRead)
                          .ToArrayAsync();

        foreach (var item in ng)
            item.HasRead = true;

        await db.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    public IQueryable<Video> GetVideoUnsafe(long id)
    {
        return db.Video.Where(e => e.Id == id);
    }

    public IQueryable<Comment> GetComment(long id, long videoId)
    {
        return db.Comments.FromSqlRaw(BasicCommentsSql).Where(c => c.VideoId == videoId && c.Id == id);
    }

    public IQueryable<Group> GetGroup(long id)
    {
        return db.Group.Where(g => g.Id == id);
    }

    public Task<bool> AreFriends(long groupId1, long groupId2)
    {
        return db.Follower.AnyAsync(f => f.FollowingId == groupId1 && f.FollowerId == groupId2 && f.IsMutual);
    }

    public Task<bool> HaveFollower(long groupId, long followerId)
    {
        return db.Follower.AnyAsync(f => f.FollowingId == groupId && f.FollowerId == followerId);
    }

    public IQueryable<long> GetFriendGroupIds(long groupId)
    {
        return db.Follower.Where(f => f.FollowingId == groupId && f.IsMutual).Select(e => e.FollowerId).Distinct();
    }

    public Task<AiGeneratedContentInfo> GetAiContent(long contentId, long groupId)
    {
        var content = db.AiGeneratedContent.Where(c => c.Id == contentId && c.GroupId == groupId && c.DeletedAt == null);
        return content.GroupJoin(db.AiGeneratedImage, c => c.AiGeneratedImageId, i => i.Id, (c, i) => new {Content = c, Images = i})
                      .SelectMany(e => e.Images.DefaultIfEmpty(), (c, i) => new {c.Content, Image = i})
                      .GroupJoin(
                           db.AiGeneratedVideo,
                           c => c.Content.AiGeneratedVideoId,
                           i => i.Id,
                           (c, v) => new {c.Content, c.Image, Videos = v}
                       )
                      .SelectMany(
                           v => v.Videos.DefaultIfEmpty(),
                           (c, v) => new AiGeneratedContentInfo
                                     {
                                         ContentId = c.Content.Id,
                                         CreatedAt = c.Content.CreatedAt,
                                         RemixedFromAiGeneratedContentId = c.Content.RemixedFromAiGeneratedContentId,
                                         Type = v == null ? AiGeneratedContentType.Image : AiGeneratedContentType.Video,
                                         Id = v == null ? c.Image.Id : v.Id,
                                         Files = v == null ? c.Image.Files : v.Files
                                     }
                       )
                      .AsNoTracking()
                      .FirstOrDefaultAsync();
    }

    private IQueryable<NotificationView> AllNotifications()
    {
        var types = Enum.GetValues(typeof(NotificationType)).Cast<NotificationType>();

        return db.Notification.Where(e => types.Contains(e.Type))
                  .GroupJoin(db.Group, n => n.DataGroupId, g => g.Id, (n, g) => new {Notification = n, Group = g})
                  .SelectMany(
                       g => g.Group.DefaultIfEmpty(),
                       (a, g) => new NotificationView
                                 {
                                     Id = a.Notification.Id,
                                     Notification = a.Notification,
                                     DataGroup = g,
                                     HasRead = db.NotificationAndGroup.Any(
                                         n => n.NotificationId == a.Notification.Id && n.HasRead
                                     )
                                 }
                   );
    }
}