using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Infrastructure.Caching.CacheKeys;
using Frever.ClientService.Contract.Ai;
using Frever.Shared.MainDb.Entities;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace Frever.Client.Core.Features.AI.UserGeneratedContent.Content.Core;

public partial class AiGeneratedContentService
{
    public async Task<AiGeneratedContentStatusDto> GetStatus(long id)
    {
        var data = await _redis.StringGetAsync(AiContentCacheKeys.DraftGenerationStatusKey(id));
        if (data.HasValue)
            return JsonConvert.DeserializeObject<AiGeneratedContentStatusDto>(data);

        var content = await repo.GetById(id).FirstOrDefaultAsync();
        return content == null ? null : new AiGeneratedContentStatusDto {Id = id, Status = content.GenerationStatus};
    }

    public async Task<AiGeneratedContentShortInfo[]> GetDrafts(AiGeneratedContentType? type, int skip, int take)
    {
        skip = Math.Clamp(skip, 0, int.MaxValue);
        take = Math.Clamp(take, 1, 50);

        var typeStr = type == null ? "" : type.ToString();

        var data = await repo.GetDraftList(currentUser)
                             .Where(c => typeStr == "" || c.Content.Type == typeStr)
                             .Where(c => c.Content.Status == AiGeneratedContent.KnownStatusDraft)
                             .Skip(skip)
                             .Take(take)
                             .ToArrayAsync();

        if (data.Length == 0)
            return [];

        foreach (var item in data)
        {
            await fileStorage.InitUrls<Group>([item.Group]);
            if (item.Image != null)
                await fileStorage.InitUrls<AiGeneratedImage>([item.Image]);
            else if (item.Video != null)
                await fileStorage.InitUrls<AiGeneratedVideo>([item.Video]);
        }

        var result = data.Select(d => new AiGeneratedContentShortInfo
                                      {
                                          Id = d.Content.Id,
                                          Group = new GroupInfo {Id = d.Group.Id, Files = d.Group.Files, NickName = d.Group.NickName},
                                          RemixedFromAiGeneratedContentId = d.Content.RemixedFromAiGeneratedContentId,
                                          Type = d.Image == null ? AiGeneratedContentType.Video : AiGeneratedContentType.Image,
                                          CreatedAt = d.Content.CreatedAt,
                                          Files = d.Image?.Files ?? d.Video?.Files ?? []
                                      }
                          )
                         .ToArray();

        return result;
    }

    public async Task<AiGeneratedContentShortInfo[]> GetFeed(long groupId, AiGeneratedContentType? type, int skip, int take)
    {
        skip = Math.Clamp(skip, 0, int.MaxValue);
        take = Math.Clamp(take, 1, 50);

        var typeStr = type == null ? "" : type.ToString();

        var data = await repo.GetPublishedList(currentUser, groupId)
                             .Where(c => typeStr == "" || c.Content.Type == typeStr)
                             .Where(c => c.Content.Status == AiGeneratedContent.KnownStatusPublished)
                             .Skip(skip)
                             .Take(take)
                             .ToArrayAsync();
        if (data.Length == 0)
            return [];

        foreach (var item in data)
        {
            await fileStorage.InitUrls<Group>([item.Group]);
            if (item.Image != null)
                await fileStorage.InitUrls<AiGeneratedImage>([item.Image]);
            else if (item.Video != null)
                await fileStorage.InitUrls<AiGeneratedVideo>([item.Video]);
        }

        var result = data.Select(d => new AiGeneratedContentShortInfo
                                      {
                                          Id = d.Content.Id,
                                          Group = new GroupInfo {Id = d.Group.Id, Files = d.Group.Files, NickName = d.Group.NickName},
                                          RemixedFromAiGeneratedContentId = d.Content.RemixedFromAiGeneratedContentId,
                                          Type = d.Image == null ? AiGeneratedContentType.Video : AiGeneratedContentType.Image,
                                          CreatedAt = d.Content.CreatedAt,
                                          Files = d.Image?.Files ?? d.Video?.Files ?? []
                                      }
                          )
                         .ToArray();

        return result;
    }

    public async Task<AiGeneratedContentFullInfo> GetById(long id)
    {
        var aiContentEntity = await repo.GetById(id).SingleOrDefaultAsync();
        if (aiContentEntity == null)
            return null;

        if (!(aiContentEntity.Status == AiGeneratedContent.KnownStatusPublished || aiContentEntity.GroupId == currentUser))
            return null;

        var resultContent = new AiGeneratedContentFullInfo();

        map.Map(aiContentEntity, resultContent);

        if (aiContentEntity.AiGeneratedImageId != null)
            resultContent.Image = await GetAiImage(aiContentEntity.AiGeneratedImageId.Value);

        if (aiContentEntity.AiGeneratedVideoId != null)
            resultContent.Video = await GetAiVideo(aiContentEntity.AiGeneratedVideoId.Value);

        return resultContent;
    }

    private async Task<AiGeneratedImageFullInfo> GetAiImage(long id)
    {
        var entity = await repo.GetAiImageById(id).SingleOrDefaultAsync();
        if (entity == null)
            return null;

        var result = new AiGeneratedImageFullInfo();

        map.Map(entity, result);

        await fileStorage.InitUrls<AiGeneratedImage>([result]);

        var persons = await repo.GetAiImagePerson(id).ToArrayAsync();

        result.Persons = persons.OrderBy(p => p.Ordinal)
                                .Select(e =>
                                        {
                                            var r = new AiGeneratedImagePersonFullInfo();
                                            map.Map(e, r);
                                            return r;
                                        }
                                 )
                                .ToList();

        await fileStorage.InitUrls<AiGeneratedImagePerson>(result.Persons);

        var sources = await repo.GetAiImageSources(id).ToArrayAsync();
        result.Sources = sources.Select(s =>
                                        {
                                            var r = new AiGeneratedImageSourceFullInfo();
                                            map.Map(s, r);
                                            return r;
                                        }
                                 )
                                .ToList();

        await fileStorage.InitUrls<AiGeneratedImageSource>(result.Sources);

        return result;
    }

    private async Task<AiGeneratedVideoFullInfo> GetAiVideo(long id)
    {
        var entity = await repo.GetAiVideoById(id).SingleOrDefaultAsync();
        if (entity == null)
            return null;

        var result = new AiGeneratedVideoFullInfo();

        map.Map(entity, result);

        await fileStorage.InitUrls<AiGeneratedVideo>([result]);

        var clips = await repo.GetAiVideoClips(entity.Id).ToArrayAsync();

        result.Clips = new List<AiGeneratedVideoClipFullInfo>();
        foreach (var c in clips.OrderBy(e => e.Ordinal))
        {
            var clip = new AiGeneratedVideoClipFullInfo();
            map.Map(c, clip);
            result.Clips.Add(clip);

            if (c.AiGeneratedImageId != null)
                clip.Image = await GetAiImage(c.AiGeneratedImageId.Value);
        }

        await fileStorage.InitUrls<AiGeneratedVideoClip>(clips);

        return result;
    }
}