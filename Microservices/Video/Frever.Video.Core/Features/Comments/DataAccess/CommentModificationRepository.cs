using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Common.Infrastructure;
using Common.Infrastructure.Database;
using Frever.Shared.MainDb;
using Frever.Shared.MainDb.Entities;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Npgsql;
using NpgsqlTypes;

namespace Frever.Video.Core.Features.Comments.DataAccess;

public interface ICommentModificationRepository
{
    IQueryable<Frever.Shared.MainDb.Entities.Video> GetVideo(long videoId);

    IQueryable<CommentGroupInfo> GetCommentGroupInfo(long videoId);

    IQueryable<Comment> GetVideoComments(long videoId);

    Task AddComment(Comment comment);

    Task<NestedTransaction> BeginTransaction();

    IQueryable<CommentLike> GetCommentLike(long videoId, long commentId, long groupId);

    Task AddCommentLike(CommentLike like);

    Task IncrementCommentLikeCount(long videoId, long commentId);

    Task<int> RemoveCommentLike(long videoId, long commentId, long groupId);

    Task DecrementCommentLikeCount(long videoId, long commentId);

    IQueryable<Frever.Shared.MainDb.Entities.Video> GetGroupVideo(long groupId);

    Task<int> SetCommentPinned(long videoId, long commentId, bool isPinned);
}

public class PersistentCommentModificationRepository(IWriteDb db) : ICommentModificationRepository
{
    public IQueryable<Frever.Shared.MainDb.Entities.Video> GetVideo(long videoId)
    {
        var videoAccess = Enum.GetValues(typeof(VideoAccess)).Cast<VideoAccess>();

        return db.Video.Where(v => videoAccess.Contains(v.Access))
                 .Where(v => !v.IsDeleted)
                 .Where(v => v.Group.DeletedAt == null && !v.Group.IsBlocked)
                 .Where(v => v.Id == videoId);
    }

    public IQueryable<CommentGroupInfo> GetCommentGroupInfo(long videoId)
    {
        return db.SqlQueryRaw<CommentGroupInfo>(Sql.CommentGroupInfo, videoId).AsNoTracking();
    }

    public async Task AddComment(Comment comment)
    {
        ArgumentNullException.ThrowIfNull(comment);

        var connection = db.GetDbConnection();
        await using var command = new NpgsqlCommand("SELECT * FROM \"AddComment\"($1, $2, $3, $4, $5)", connection);

        command.Parameters.AddWithValue(NpgsqlDbType.Bigint, comment.VideoId);
        command.Parameters.AddWithValue(NpgsqlDbType.Bigint, comment.GroupId);
        command.Parameters.AddWithValue(NpgsqlDbType.Text, comment.Text);
        command.Parameters.AddWithValue(
            NpgsqlDbType.Json,
            JsonConvert.SerializeObject(comment.Mentions, ValueConversionExtensions.Settings)
        );
        command.Parameters.AddWithValue(NpgsqlDbType.Bigint, comment.ReplyToCommentId ?? (object) DBNull.Value);

        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync();

        comment.Id = (long) await command.ExecuteScalarAsync();
    }

    public Task<NestedTransaction> BeginTransaction()
    {
        return db.BeginTransactionSafe();
    }

    public IQueryable<CommentLike> GetCommentLike(long videoId, long commentId, long groupId)
    {
        return db.CommentLikes.Where(c => c.VideoId == videoId && c.CommentId == commentId && c.GroupId == groupId);
    }

    public IQueryable<Comment> GetVideoComments(long videoId)
    {
        return db.Comments.FromSqlRaw(Sql.BasicCommentsSql, videoId).AsNoTracking();
    }

    public async Task AddCommentLike(CommentLike like)
    {
        ArgumentNullException.ThrowIfNull(like);

        await db.CommentLikes.AddAsync(like);
        await db.SaveChangesAsync();
    }

    public async Task<int> RemoveCommentLike(long videoId, long commentId, long groupId)
    {
        return await db.ExecuteSqlRawAsync(
                   """delete from "CommentLikes" where "CommentId" = {0} and "VideoId" = {1} and "GroupId" = {2}""",
                   commentId,
                   videoId,
                   groupId
               );
    }

    public async Task IncrementCommentLikeCount(long videoId, long commentId)
    {
        await db.ExecuteSqlRawAsync(
            """
            update "Comments"
            set "LikeCount" = "LikeCount" + 1
            where
            "Id" = {0} and "VideoId" = {1}
            """,
            commentId,
            videoId
        );
    }

    public async Task DecrementCommentLikeCount(long videoId, long commentId)
    {
        await db.ExecuteSqlRawAsync(
            """
            update "Comments"
            set "LikeCount" = "LikeCount" - 1
            where
            "Id" = {0} and "VideoId" = {1}
            """,
            commentId,
            videoId
        );
    }

    public IQueryable<Frever.Shared.MainDb.Entities.Video> GetGroupVideo(long groupId)
    {
        return db.Video.Where(v => v.GroupId == groupId);
    }

    public Task<int> SetCommentPinned(long videoId, long commentId, bool isPinned)
    {
        return db.ExecuteSqlRawAsync(
            """
            update "Comments"
            set "IsPinned" = {0}
            where
            "Id" = {1} and "VideoId" = {2}
            """,
            isPinned,
            commentId,
            videoId
        );
    }
}