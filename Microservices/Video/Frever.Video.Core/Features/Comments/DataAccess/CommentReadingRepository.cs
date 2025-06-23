using System;
using System.Linq;
using Frever.Shared.MainDb;
using Frever.Shared.MainDb.Entities;
using Microsoft.EntityFrameworkCore;

namespace Frever.Video.Core.Features.Comments.DataAccess;

public interface ICommentReadingRepository
{
    IQueryable<Comment> GetVideoComments(long videoId);

    IQueryable<Comment> GetRootComments(long videoId, string key, int takeOlder, int takeNewer);

    /// <summary>
    ///     Gets flatten replies to certain root comment.
    ///     Note that replies are sorted from older to newer.
    ///     Newer comments has bigger Thread value.
    ///     If <paramref name="key" /> is not specified
    /// </summary>
    IQueryable<Comment> GetThreadCommentRange(
        long videoId,
        string rootCommentKey,
        string key,
        int takeOlder,
        int takeNewer,
        long[] blockedGroups
    );

    IQueryable<CommentLike> GetVideoCommentLikes(long videoId, long groupId);

    IQueryable<CommentGroupInfo> GetCommentGroupInfo(long videoId);
}

public class PersistentCommentReadingRepository(IWriteDb db) : ICommentReadingRepository
{
    public IQueryable<Comment> GetVideoComments(long videoId)
    {
        return db.Comments.FromSqlRaw(Sql.BasicCommentsSql, videoId).AsNoTracking();
    }

    /// <summary>
    ///     Gets the root comments.
    ///     Note the root comments are sorted from newer to older
    /// </summary>
    public IQueryable<Comment> GetRootComments(long videoId, string key, int takeOlder, int takeNewer)
    {
        const string rootCommentsSql = Sql.BasicCommentsSql + """ and "ReplyToCommentId" is null """;

        if (string.IsNullOrWhiteSpace(key)) // Take the newest comments
            return db.Comments.FromSqlRaw(rootCommentsSql, videoId).OrderByDescending(c => c.Id).AsNoTracking().Take(takeOlder);

        var olderComments = db.Comments.FromSqlRaw(rootCommentsSql + """ and "Thread" <= {1}::ltree""", videoId, key);

        var newerComments = db.Comments.FromSqlRaw(rootCommentsSql + """ and "Thread" > {1}::ltree""", videoId, key);

        return newerComments.OrderBy(a => a.Id)
                            .Take(takeNewer)
                            .Concat(olderComments.OrderByDescending(a => a.Id).Take(takeOlder + 1))
                            .OrderByDescending(a => a.Id)
                            .AsNoTracking();
    }

    /// <summary>
    ///     Gets flatten replies to certain root comment.
    ///     Note that replies are sorted from older to newer.
    ///     Newer comments has bigger Thread value.
    ///     If <paramref name="key" /> is not specified
    /// </summary>
    public IQueryable<Comment> GetThreadCommentRange(
        long videoId,
        string rootCommentKey,
        string key,
        int takeOlder,
        int takeNewer,
        long[] blockedGroups
    )
    {
        if (string.IsNullOrWhiteSpace(rootCommentKey))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(rootCommentKey));

        if (blockedGroups == null || blockedGroups.Length == 0)
            blockedGroups = [-1]; // To don't filter out all records on empty array

        const string threadCommentsSql = Sql.BasicCommentsSql +
                                         """ and c."Thread" <@ {1}::ltree and c."Thread" <> {1}::ltree and c."GroupId" <> ANY({2})""";

        if (string.IsNullOrWhiteSpace(key)) // Take oldest comments
            return db.Comments.FromSqlRaw(
                          threadCommentsSql + """ order by "Thread" limit {3} """,
                          videoId,
                          rootCommentKey,
                          blockedGroups,
                          takeNewer
                      )
                     .AsNoTracking();

        var olderComments = db.Comments.FromSqlRaw(
            threadCommentsSql + """ and c."Thread" < {3}::ltree order by c."Thread" desc limit {4} """,
            videoId,
            rootCommentKey,
            blockedGroups,
            key,
            takeOlder
        );

        var newerComments = db.Comments.FromSqlRaw(
            threadCommentsSql + """ and c."Thread" >= {3}::ltree order by c."Thread" limit {4}""",
            videoId,
            rootCommentKey,
            blockedGroups,
            key,
            takeNewer
        );

        return newerComments.Concat(olderComments).OrderBy(a => a.Thread).AsNoTracking();
    }

    public IQueryable<CommentLike> GetVideoCommentLikes(long videoId, long groupId)
    {
        return db.CommentLikes.Where(cl => cl.VideoId == videoId && cl.GroupId == groupId);
    }

    public IQueryable<CommentGroupInfo> GetCommentGroupInfo(long videoId)
    {
        return db.SqlQueryRaw<CommentGroupInfo>(Sql.CommentGroupInfo, videoId).AsNoTracking();
    }
}