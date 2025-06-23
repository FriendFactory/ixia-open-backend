using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Frever.Shared.MainDb;
using Frever.Shared.MainDb.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Frever.Video.Core.Features.Manipulation.DataAccess;

public interface IVideoManipulationRepository
{
    IQueryable<Frever.Shared.MainDb.Entities.Video> GetVideoById(long id);
    IQueryable<Frever.Shared.MainDb.Entities.Video> GetPinnedVideo(long groupId);
    Task<bool> LikeVideo(long videoId, long userId);
    Task<bool> UnlikeVideo(long videoId, long userId);
    Task<bool> UpdateVideoAccess(long videoId, VideoAccess videoAccess, long[] taggedFriendIds);
    Task<long[]> GetTaggedFriends(long videoId);
    Task<int> SaveChanges();
    Task UpdateVideoParams(long videoId, bool? allowRemix, bool? allowComment);
    Task SetVideoLinks(long videoId, Dictionary<string, string> links);
    Task MarkOwnVideoAsDeleted(long videoId);
}

public class PersistentVideoManipulationRepository(IWriteDb db, ILogger<PersistentVideoManipulationRepository> log)
    : IVideoManipulationRepository
{
    public IQueryable<Frever.Shared.MainDb.Entities.Video> GetVideoById(long id)
    {
        var videoAccess = Enum.GetValues(typeof(VideoAccess)).Cast<VideoAccess>();
        return db.Video.Where(v => videoAccess.Contains(v.Access))
                 .Where(v => !v.IsDeleted)
                 .Where(v => v.Group.DeletedAt == null && !v.Group.IsBlocked)
                 .Where(v => v.Id == id);
    }

    public async Task<bool> LikeVideo(long videoId, long userId)
    {
        var likeAdded = false;

        await using var transaction = await db.BeginTransactionSafe();

        var likeRecord = await db.Like.FirstOrDefaultAsync(l => l.VideoId == videoId && l.UserId == userId);

        if (likeRecord == null)
        {
            likeRecord = new Like {VideoId = videoId, UserId = userId};
            await db.Like.AddAsync(likeRecord);

            likeAdded = true;
        }

        likeRecord.Time = DateTime.UtcNow;

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException e)
        {
            log.LogWarning(e, $"Failed to like video {videoId} by user {userId}");
        }

        await transaction.Commit();

        return likeAdded;
    }

    public async Task<bool> UnlikeVideo(long videoId, long userId)
    {
        await using var transaction = await db.BeginTransaction();
        var likeRecord = await db.Like.FirstOrDefaultAsync(l => l.UserId == userId && l.VideoId == videoId);

        if (likeRecord != null)
        {
            db.Like.Remove(likeRecord);
            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException e)
            {
                log.LogWarning(e, $"Failed to unlike video {videoId} by user {userId}");
            }

            await transaction.CommitAsync();
            return true;
        }

        return false;
    }

    public async Task<bool> UpdateVideoAccess(long videoId, VideoAccess videoAccess, long[] taggedFriendIds)
    {
        var video = await db.Video.FindAsync(videoId);
        if (video == null)
            throw new InvalidOperationException($"Video {videoId} is not found");

        var wereUpdated = video.Access != videoAccess;

        video.Access = videoAccess;

        if (videoAccess == VideoAccess.ForTaggedGroups)
        {
            var allExistingVideoTags = await db.VideoGroupTag.Where(t => t.VideoId == videoId).ToArrayAsync();

            var taggedFriends = taggedFriendIds.Distinct()
                                               .Where(e => !allExistingVideoTags.Any(i => i.IsCharacterTag && e == i.GroupId))
                                               .Select(e => new VideoGroupTag {GroupId = e, VideoId = videoId})
                                               .ToArray();

            var nonCharacterTags = allExistingVideoTags.Where(e => !e.IsCharacterTag).ToArray();

            await UpdateTaggedGroups(taggedFriends, nonCharacterTags);
        }

        await db.SaveChangesAsync();

        return wereUpdated;
    }

    public async Task UpdateVideoParams(long videoId, bool? allowRemix, bool? allowComment)
    {
        if (allowComment == null && allowRemix == null)
            return;

        var video = await db.Video.FirstOrDefaultAsync(v => v.Id == videoId);
        if (video != null)
        {
            if (allowRemix != null)
                video.AllowRemix = allowRemix.Value;

            if (allowComment != null)
                video.AllowComment = allowComment.Value;

            await db.SaveChangesAsync();
        }
    }

    public async Task SetVideoLinks(long videoId, Dictionary<string, string> links)
    {
        var video = await db.Video.FirstOrDefaultAsync(v => v.Id == videoId);
        if (video != null)
        {
            video.Links = (links?.Count ?? 0) > 0 ? links : null;
            await db.SaveChangesAsync();
        }
    }

    public IQueryable<Frever.Shared.MainDb.Entities.Video> GetPinnedVideo(long groupId)
    {
        return db.Video.Where(v => v.GroupId == groupId && v.PinOrder != null);
    }

    public Task<long[]> GetTaggedFriends(long videoId)
    {
        return db.VideoGroupTag.Where(e => e.VideoId == videoId && !e.IsCharacterTag).Select(e => e.GroupId).ToArrayAsync();
    }

    public Task<int> SaveChanges()
    {
        return db.SaveChangesAsync();
    }

    public async Task MarkOwnVideoAsDeleted(long videoId)
    {
        var video = await db.Video.FindAsync(videoId);
        if (video == null)
            throw new InvalidOperationException($"Video {videoId} is not found");

        video.IsDeleted = true;
        video.DeletedByGroupId = video.GroupId;

        await db.SaveChangesAsync();
    }

    private async Task UpdateTaggedGroups(VideoGroupTag[] newVideoTags, VideoGroupTag[] existingVideoTags)
    {
        var toAdd = newVideoTags.Where(e => existingVideoTags.All(g => g.GroupId != e.GroupId));
        await db.VideoGroupTag.AddRangeAsync(toAdd);

        var toUpdate = existingVideoTags.Where(e => !e.IsCharacterTag && newVideoTags.Any(g => g.GroupId == e.GroupId && g.IsCharacterTag));
        foreach (var item in toUpdate)
            item.IsCharacterTag = true;

        var toRemove = existingVideoTags.Where(e => newVideoTags.All(t => t.GroupId != e.GroupId));
        db.VideoGroupTag.RemoveRange(toRemove);
    }
}