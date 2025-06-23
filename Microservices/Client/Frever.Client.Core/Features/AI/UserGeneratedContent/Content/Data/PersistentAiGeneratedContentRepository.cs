using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Infrastructure.Database;
using Frever.Shared.MainDb;
using Frever.Shared.MainDb.Entities;
using Microsoft.EntityFrameworkCore;

namespace Frever.Client.Core.Features.AI.UserGeneratedContent.Content.Data;

public class PersistentAiGeneratedContentRepository(IWriteDb db) : IAiGeneratedContentRepository
{
    public IQueryable<AiGeneratedContentFullData> GetOwnAiContent(long groupId)
    {
        return GetAvailableAiContent(groupId, groupId).Where(c => c.Content.GroupId == groupId);
    }

    public IQueryable<AiGeneratedContentFullData> GetDraftList(long groupId)
    {
        return GetAvailableAiContent(groupId, groupId)
              .Where(c => c.Content.GroupId == groupId)
              .Where(c => c.Content.Status == AiGeneratedContent.KnownStatusDraft)
              .Where(c => c.Content.GenerationStatus == AiGeneratedContent.KnownGenerationStatusCompleted)
              .OrderByDescending(x => x.Content.Id)
              .AsNoTracking();
    }

    public IQueryable<AiGeneratedContentFullData> GetPublishedList(long currentGroupId, long groupId)
    {
        return GetAvailableAiContent(currentGroupId, groupId)
              .Where(c => c.Content.GroupId == groupId)
              .Where(c => c.Content.Status == AiGeneratedContent.KnownStatusPublished)
              .Where(c => c.Content.GenerationStatus == AiGeneratedContent.KnownGenerationStatusCompleted)
              .OrderByDescending(c => c.Content.Id)
              .AsNoTracking();
    }

    public IQueryable<AiGeneratedContent> GetById(long id)
    {
        return db.AiGeneratedContent.Where(c => c.DeletedAt == null).Where(c => c.Id == id);
    }

    public IQueryable<AiGeneratedImage> GetOwnAiImageById(long id, long groupId)
    {
        return GetAiImageById(id).Where(i => i.GroupId == groupId);
    }

    public IQueryable<AiGeneratedImage> GetAiImageById(long id)
    {
        return db.AiGeneratedImage.Where(i => i.Id == id);
    }

    public IQueryable<AiGeneratedImagePerson> GetAiImagePerson(long aiGeneratedImageId)
    {
        return db.AiGeneratedImagePerson.Where(p => p.AiGeneratedImageId == aiGeneratedImageId);
    }

    public IQueryable<AiGeneratedImageSource> GetAiImageSources(long aiGeneratedImageId)
    {
        return db.AiGeneratedImageSource.Where(s => s.AiGeneratedImageId == aiGeneratedImageId);
    }

    public IQueryable<AiCharacterImage> GetAiCharacterImage(long aiCharacterImageId)
    {
        return db.AiCharacterImage.Where(i => i.Id == aiCharacterImageId);
    }

    public IQueryable<AiGeneratedVideo> GetOwnAiVideoById(long id, long groupId)
    {
        return GetAiVideoById(id).Where(v => v.GroupId == groupId);
    }

    public IQueryable<AiGeneratedVideo> GetAiVideoById(long id)
    {
        return db.AiGeneratedVideo.Where(v => v.Id == id);
    }

    public IQueryable<AiGeneratedVideoClip> GetAiVideoClips(long aiGeneratedVideoId)
    {
        return db.AiGeneratedVideoClip.Where(c => c.AiGeneratedVideoId == aiGeneratedVideoId);
    }

    public Task<AiGeneratedImage> GetContentImage(long aiContentId, long groupId)
    {
        return db.AiGeneratedContent.Where(e => e.Id == aiContentId && e.GroupId == groupId && e.DeletedAt == null)
                 .Select(e => e.AiGeneratedVideoId == null
                                  ? e.GeneratedImage
                                  : e.GeneratedVideo.GeneratedVideoClip.Select(c => c.GeneratedImage).FirstOrDefault()
                  )
                 .AsNoTracking()
                 .FirstOrDefaultAsync();
    }

    public TEntity Add<TEntity>(TEntity entity)
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(entity);
        db.Set<TEntity>().Add(entity);
        return entity;
    }

    public IEnumerable<TEntity> AddRange<TEntity>(IEnumerable<TEntity> entities)
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(entities);
        var data = entities.ToArray();
        db.Set<TEntity>().AddRange(data);
        return data;
    }

    public TEntity Remove<TEntity>(TEntity entity)
        where TEntity : class
    {
        db.Set<TEntity>().Remove(entity);
        return entity;
    }

    public async Task DeleteVideoByAiContentId(long aiContentId)
    {
        var aiContent = await db.AiGeneratedContent.FirstOrDefaultAsync(c => c.Id == aiContentId);
        var video = await db.Video.Where(v => v.AiContentId == aiContentId).ToListAsync();
        foreach (var v in video)
        {
            v.IsDeleted = true;

            if (aiContent.Type == AiGeneratedContent.KnownTypeImage)
                // db.Video.Remove(v); // not working
                // Cannot delete from Video table because of error: 55000: deleting from table "GranularLikes_2021" is not possible, because it miss replica identifier and it publishes deleting
                v.AiContentId = null;
        }

        await db.SaveChangesAsync();
    }

    public Task<int> SaveChanges()
    {
        return db.SaveChangesAsync();
    }

    public Task<NestedTransaction> BeginTransaction()
    {
        return db.BeginTransactionSafe();
    }

    private IQueryable<AiGeneratedContentFullData> GetAvailableAiContent(long currentGroupId, long groupId)
    {
        var all = db.AiGeneratedContent.Where(c => c.DeletedAt == null);
        var availableVideo = db.GetGroupAvailableVideoQuery(groupId, currentGroupId).Result;

        var aiVideos = all.Where(c => c.Type == AiGeneratedContent.KnownTypeVideo)
                          .Select(
                               c => new AiGeneratedContentFullData
                                    {
                                        Content = c, IsPublished = availableVideo.Any(v => v.AiContentId == c.Id)
                                    }
                           );

        var images = all.Where(c => c.Type == AiGeneratedContent.KnownTypeImage)
                        .Select(c => new AiGeneratedContentFullData {Content = c, IsPublished = db.Video.Any(v => v.AiContentId == c.Id)});

        return aiVideos.Concat(images)
                       .Join(
                            db.Group,
                            c => c.Content.GroupId,
                            g => g.Id,
                            (c, g) => new AiGeneratedContentFullData {Content = c.Content, Group = g, IsPublished = c.IsPublished}
                        )
                       .GroupJoin(
                            db.AiGeneratedImage,
                            c => c.Content.AiGeneratedImageId,
                            i => i.Id,
                            (c, im) => new {Content = c, Images = im}
                        )
                       .SelectMany(
                            x => x.Images.DefaultIfEmpty(),
                            (c, image) => new AiGeneratedContentFullData
                                          {
                                              Content = c.Content.Content,
                                              Group = c.Content.Group,
                                              Image = image,
                                              IsPublished = c.Content.IsPublished
                                          }
                        )
                       .GroupJoin(db.AiGeneratedVideo, c => c.Content.AiGeneratedVideoId, i => i.Id, (c, v) => new {Content = c, Video = v})
                       .SelectMany(
                            x => x.Video.DefaultIfEmpty(),
                            (c, video) => new AiGeneratedContentFullData
                                          {
                                              Content = c.Content.Content,
                                              Group = c.Content.Group,
                                              Image = c.Content.Image,
                                              Video = video,
                                              IsPublished = c.Content.IsPublished
                                          }
                        );
    }
}