using System;
using System.Linq;
using System.Threading.Tasks;
using Common.Infrastructure;
using Frever.Shared.MainDb;
using Frever.Shared.MainDb.Entities;
using Microsoft.EntityFrameworkCore;

namespace Frever.AdminService.Core.Services.VideoModeration.DataAccess;

public class PersistentVideoRepository(IWriteDb db) : IVideoRepository
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

    private readonly IWriteDb _db = db ?? throw new ArgumentNullException(nameof(db));

    public IQueryable<Shared.MainDb.Entities.Video> GetVideos()
    {
        return _db.Video;
    }

    public IQueryable<VideoKpi> GetVideoKpi()
    {
        return _db.VideoKpi;
    }

    public IQueryable<Group> GetGroups()
    {
        return _db.Group;
    }

    public IQueryable<Shared.MainDb.Entities.Video> GetVideosByHashtagId(long hashtagId)
    {
        return _db.VideoAndHashtag.Where(e => e.HashtagId == hashtagId).Select(e => e.Video);
    }

    public IQueryable<Comment> GetComments()
    {
        return _db.Comments.FromSqlRaw(BasicCommentsSql).AsNoTracking();
    }

    public Task PublishVideo(long videoId)
    {
        return ChangeVideoPublicState(videoId, true);
    }

    public Task UnPublishVideo(long videoId)
    {
        return ChangeVideoPublicState(videoId, false);
    }

    public async Task SetVideoDeleted(long videoId, long groupId, bool isDeleted)
    {
        var video = await _db.Video.FindAsync(videoId);
        if (video == null)
            throw new InvalidOperationException($"Video {videoId} is not found");

        video.IsDeleted = isDeleted;
        video.DeletedByGroupId = isDeleted ? groupId : null;

        await _db.SaveChangesAsync();
    }

    public Task MarkAccountVideosAsDeleted(long groupId)
    {
        return _db.ExecuteSqlInterpolatedAsync($"update \"Video\" set \"IsDeleted\" = true where \"GroupId\" = {groupId}");
    }

    public Task EraseAccountComments(long groupId)
    {
        return _db.ExecuteSqlInterpolatedAsync($"update \"Comments\" set \"Text\" = '' where \"GroupId\" = {groupId}");
    }

    public async Task SetCommentDeleted(long videoId, long commentId, bool isDeleted)
    {
        await using var transaction = await _db.BeginTransaction();

        var comment = await GetComments().FirstOrDefaultAsync(e => e.Id == commentId && e.VideoId == videoId);

        if (comment == null)
            throw AppErrorWithStatusCodeException.NotFound("Comment is not found", "VideoCommentNotFound");

        var updatedCount = await _db.ExecuteSqlRawAsync(
                               @"update ""Comments"" set ""IsDeleted"" = {0} where ""Id"" = {1} and ""VideoId"" = {2}",
                               isDeleted,
                               comment.Id,
                               comment.VideoId
                           );

        if (updatedCount > 0)
            await _db.ExecuteSqlRawAsync(
                @"
                    update ""Comments""
                    set ""ReplyCount"" = ""ReplyCount"" + {2}
                    where ""VideoId"" = {0} 
                    and ""Id"" in 
                        (
                            select ""Id""
                            from ""Comments""
                            where ""VideoId"" = {0}
                            and ""Thread"" @> (select ""Thread""
                                                from ""Comments""
                                                where ""VideoId"" = {0}
                                                and ""Id"" = {1})
                            and ""Id"" <> {1}
                        )                  
                    ",
                comment.VideoId,
                comment.Id,
                isDeleted ? -1 : 1
            );

        await transaction.CommitAsync();
    }

    public Task SaveChanges()
    {
        return _db.SaveChangesAsync();
    }

    private async Task ChangeVideoPublicState(long videoId, bool isPublic)
    {
        var video = await _db.Video.FindAsync(videoId);

        video.Access = isPublic ? VideoAccess.Public : VideoAccess.Private;

        await _db.SaveChangesAsync();
    }
}