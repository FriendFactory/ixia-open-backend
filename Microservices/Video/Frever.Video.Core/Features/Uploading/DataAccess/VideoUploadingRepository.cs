using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Frever.Shared.MainDb;
using Frever.Shared.MainDb.Entities;
using Microsoft.EntityFrameworkCore;

namespace Frever.Video.Core.Features.Uploading.DataAccess;

public interface IVideoUploadingRepository
{
    Task<Frever.Shared.MainDb.Entities.Video> CreateOrReplaceVideoAsync(
        Frever.Shared.MainDb.Entities.Video newVideo,
        IReadOnlyList<string> hashtags,
        IReadOnlyList<string> mentions,
        VideoGroupTag[] groupTags
    );
    IQueryable<Language> GetGroupLanguage(long groupId);
    IQueryable<Country> GetGroupCountry(long groupId);
    IQueryable<AiGeneratedVideo> GetAiVideo(long aiVideoId);
}

public class PersistentVideoUploadingRepository(IWriteDb db) : IVideoUploadingRepository
{
    public IQueryable<Language> GetGroupLanguage(long groupId)
    {
        return db.Language.Where(l => db.Group.Where(g => g.Id == groupId).Any(g => g.DefaultLanguageId == l.Id));
    }

    public IQueryable<Country> GetGroupCountry(long groupId)
    {
        return db.Country.Where(c => db.Group.Where(g => g.Id == groupId).Any(g => g.TaxationCountryId == c.Id));
    }

    public IQueryable<Group> GetGroupByIds(IEnumerable<long> groupIds)
    {
        return db.Group.Where(g => !g.IsBlocked && g.DeletedAt == null).Where(e => groupIds.Contains(e.Id));
    }

    public async Task<Frever.Shared.MainDb.Entities.Video> CreateOrReplaceVideoAsync(
        Frever.Shared.MainDb.Entities.Video newVideo,
        IReadOnlyList<string> hashtags,
        IReadOnlyList<string> mentions,
        VideoGroupTag[] groupTags
    )
    {
        await using var transaction = await db.BeginTransactionSafe();

        var hashtagIds = await CreateHashtagsIfDoNotExistAndGetAllIds(hashtags);
        newVideo.VideoAndHashtag = hashtagIds.Select(e => new VideoAndHashtag {HashtagId = e}).ToList();

        newVideo.VideoMentions = await GetGroupByIds(mentions.Select(long.Parse))
                                      .Select(e => new VideoMention {GroupId = e.Id})
                                      .ToListAsync();

        var video = await CreateOrReplaceVideo(newVideo, groupTags);

        await transaction.Commit();

        return video;
    }

    public async Task<Frever.Shared.MainDb.Entities.Video> CreateOrReplaceVideo(
        Frever.Shared.MainDb.Entities.Video video,
        VideoGroupTag[] groupTags
    )
    {
        ArgumentNullException.ThrowIfNull(video);

        var entry = video;
        entry.CreatedTime = DateTime.UtcNow;
        await db.Video.AddAsync(entry);

        if (entry.VerticalCategoryId == 0)
            entry.VerticalCategoryId = await db.VerticalCategory.OrderBy(e => e.Id).Select(e => e.Id).FirstAsync();

        if (entry.PlatformId == 0)
            entry.PlatformId = await db.Platform.OrderBy(e => e.Id).Select(e => e.Id).FirstAsync();

        await db.SaveChangesAsync();

        var existingVideoTags = await db.VideoGroupTag.Where(t => t.VideoId == entry.Id).ToArrayAsync();

        foreach (var item in groupTags)
            item.VideoId = entry.Id;

        await UpdateTaggedGroups(groupTags, existingVideoTags);

        return entry;
    }

    public IQueryable<AiGeneratedVideo> GetAiVideo(long aiVideoId)
    {
        return db.AiGeneratedVideo.Where(c => c.Id == aiVideoId);
    }

    private async Task<IReadOnlyList<long>> CreateHashtagsIfDoNotExistAndGetAllIds(IReadOnlyList<string> hashtags)
    {
        var hashtagsToLower = hashtags.Select(e => e.ToLower());

        var existingHashtags = await db.Hashtag.Where(e => hashtagsToLower.Contains(e.Name.ToLower())).AsNoTracking().ToListAsync();

        var hashtagsToCreate = hashtags.Where(e => existingHashtags.All(h => !h.Name.Equals(e, StringComparison.OrdinalIgnoreCase)))
                                       .GroupBy(e => e.ToLower())
                                       .Select(e => new Hashtag {Name = e.First()})
                                       .ToList();

        db.Hashtag.AddRange(hashtagsToCreate);
        await db.SaveChangesAsync();

        return existingHashtags.Select(e => e.Id).Concat(hashtagsToCreate.Select(e => e.Id)).Distinct().ToList();
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

        await db.SaveChangesAsync();
    }
}