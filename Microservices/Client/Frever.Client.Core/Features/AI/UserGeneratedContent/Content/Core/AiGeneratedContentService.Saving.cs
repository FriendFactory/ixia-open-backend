using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthServerShared;
using AutoMapper;
using Common.Infrastructure;
using Common.Infrastructure.Caching.CacheKeys;
using FluentValidation;
using Frever.Client.Core.Features.AI.Moderation;
using Frever.Client.Core.Features.AI.UserGeneratedContent.Content.Data;
using Frever.Client.Core.Features.AI.UserGeneratedContent.Content.Input;
using Frever.Client.Core.Features.Sounds.UserSounds;
using Frever.Client.Shared.Files;
using Frever.Shared.MainDb.Entities;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace Frever.Client.Core.Features.AI.UserGeneratedContent.Content.Core;

public partial class AiGeneratedContentService(
    IMapper map,
    UserInfo currentUser,
    IConnectionMultiplexer redisConnection,
    IAiGeneratedContentRepository repo,
    IFileStorageService fileStorage,
    IAiContentModerationService moderationService,
    IUserSoundAssetService userSoundService,
    IValidator<AiGeneratedContentInput> contentMetadataValidator
) : IAiGeneratedContentService
{
    private readonly IDatabase _redis = redisConnection.GetDatabase();

    public async Task<AiGeneratedContentFullInfo> SaveDraft(AiGeneratedContentInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return await SaveCore(
                   input,
                   entity =>
                   {
                       entity.Status = AiGeneratedContent.KnownStatusDraft;
                       entity.GenerationStatus = AiGeneratedContent.KnownGenerationStatusCompleted;
                   }
               );
    }

    public async Task<long> SaveDraftInternal(AiGenerationInput input, bool hideResult = false)
    {
        ArgumentNullException.ThrowIfNull(input);

        var result = await SaveCore(
                         input.ToContentInput(),
                         entity =>
                         {
                             entity.Status = hideResult ? AiGeneratedContent.KnownStatusHidden : AiGeneratedContent.KnownStatusDraft;
                             entity.GenerationStatus = AiGeneratedContent.KnownGenerationStatusInProgress;
                         },
                         false
                     );

        return result.Id;
    }

    public async Task SetGenerationInfo(long id, string generationKey, AiContentGenerationParameters parameters)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(generationKey);

        var existing = await repo.GetById(id).FirstOrDefaultAsync(c => c.GroupId == currentUser.UserMainGroupId);
        if (existing == null)
            throw AppErrorWithStatusCodeException.NotFound("Not found", "NOT_FOUND");

        if (existing.GenerationKey != null)
            return;

        existing.GenerationKey = generationKey;
        existing.GenerationParameters = parameters;
        await repo.SaveChanges();

        await _redis.StringSetAsync(
            AiContentCacheKeys.DraftGenerationStatusKey(id),
            JsonConvert.SerializeObject(new AiGeneratedContentStatusDto {Id = id, Status = AiGeneratedContent.KnownGenerationStatusInProgress}),
            TimeSpan.FromMinutes(15)
        );
    }

    public async Task Delete(long id)
    {
        var existing = await repo.GetById(id).FirstOrDefaultAsync(c => c.GroupId == currentUser.UserMainGroupId);
        if (existing == null)
            throw AppErrorWithStatusCodeException.NotFound("Not found", "NOT_FOUND");

        existing.DeletedAt = DateTime.UtcNow;

        if (existing.GenerationStatus == AiGeneratedContent.KnownGenerationStatusInProgress)
            existing.GenerationStatus = AiGeneratedContent.KnownGenerationStatusFailed;

        await repo.SaveChanges();
        await repo.DeleteVideoByAiContentId(id);
    }

    private async Task<AiGeneratedContentFullInfo> SaveCore(
        AiGeneratedContentInput input,
        Action<AiGeneratedContent> doExtraChanges = null,
        bool withValidation = true
    )
    {
        ArgumentNullException.ThrowIfNull(input);

        if (withValidation)
            await contentMetadataValidator.ValidateAndThrowAsync(input);

        if (input.Id != 0)
            if (!await repo.GetById(input.Id).AnyAsync(e => e.GroupId == currentUser.UserMainGroupId))
                throw AppErrorWithStatusCodeException.NotAuthorized("Cannot modify data of other users", "AiContent_ModifyOthersData");

        var uploader = fileStorage.CreateFileUploader();

        var image = default(AiGeneratedImage);
        var video = default(AiGeneratedVideo);

        await using var transaction = await repo.BeginTransaction();

        if (input.Image != null)
            image = await GetOrCreateAiImage(input.Image, uploader);

        if (input.Video != null)
        {
            video = await GetOrCreateAiVideo(input.Video);
            video.LengthSec = input.Video.Clips.Sum(c => c.LengthSec);

            await repo.SaveChanges();

            await uploader.UploadFiles<AiGeneratedVideo>(video);

            var allClips = new List<AiGeneratedVideoClip>();
            var ordinal = 1;
            foreach (var clip in input.Video.Clips)
            {
                var imageId = await GetOrCreateImageId(input, clip, uploader);
                var clipEntity = await GetOrCreateAiVideoClip(video.Id, clip);
                clipEntity.Ordinal = ordinal;
                clipEntity.AiGeneratedImageId = imageId;

                allClips.Add(clipEntity);
                ordinal++;
            }

            await repo.SaveChanges();

            await uploader.UploadFilesAll<AiGeneratedVideoClip>(allClips);
        }

        var aiContent = await GetOrCreateAiContent(input);

        aiContent.AiGeneratedImageId = image?.Id;
        aiContent.AiGeneratedVideoId = video?.Id;
        aiContent.ExternalSongId = video?.ExternalSongId;
        aiContent.IsLipSync = video?.IsLipSync;
        aiContent.CreatedAt = DateTime.UtcNow;

        doExtraChanges?.Invoke(aiContent);

        await repo.SaveChanges();
        await transaction.Commit();

        await uploader.WaitForCompletion();

        return await GetById(aiContent.Id);
    }

    private async Task<long?> GetOrCreateImageId(AiGeneratedContentInput input, AiGeneratedVideoClipInput clip, IFileUploader uploader)
    {
        if (clip.Image != null)
        {
            var image1 = await GetOrCreateAiImage(clip.Image, uploader);
            return image1.Id;
        }

        if (clip.RefAiImageId == null && input.RemixedFromAiGeneratedContentId == null)
            return null;

        var image = clip.RefAiImageId == null
                        ? await repo.GetContentImage(input.RemixedFromAiGeneratedContentId.Value, currentUser)
                        : await repo.GetAiImageById(clip.RefAiImageId.Value)
                                    .AsNoTracking()
                                    .FirstOrDefaultAsync(e => e.GroupId == currentUser);

        if (image == null)
            throw AppErrorWithStatusCodeException.NotFound("Image not found", "ERROR_IMAGE_NOT_FOUND");

        return await CopyImage(image, uploader);
    }

    private async Task<AiGeneratedContent> GetOrCreateAiContent(AiGeneratedContentInput input)
    {
        var entity = input.Id == 0
                         ? repo.Add(new AiGeneratedContent {CreatedAt = DateTime.UtcNow, GroupId = currentUser})
                         : await repo.GetById(input.Id).SingleAsync();

        map.Map(input, entity);

        return entity;
    }

    private async Task<AiGeneratedImage> GetOrCreateAiImage(AiGeneratedImageInput input, IFileUploader uploader)
    {
        var entity = input.Id == 0
                         ? repo.Add(new AiGeneratedImage {GroupId = currentUser.UserMainGroupId})
                         : await repo.GetOwnAiImageById(input.Id, currentUser).SingleAsync();

        map.Map(input, entity);
        entity.NumOfCharacters = input.Persons?.Count ?? 1;

        await repo.SaveChanges();

        await uploader.UploadFiles<AiGeneratedImage>(entity);

        var persons = await GetOrCreateAiImagePersons(entity.Id, input.Persons ?? []);
        var sources = await GetOrCreateAiImageSources(entity.Id, input.Sources ?? []);

        await repo.SaveChanges();

        await uploader.UploadFilesAll<AiGeneratedImagePerson>(persons);
        await uploader.UploadFilesAll<AiGeneratedImageSource>(sources);

        return entity;
    }

    private async Task<List<AiGeneratedImagePerson>> GetOrCreateAiImagePersons(
        long aiImageId,
        IEnumerable<AiGeneratedImagePersonInput> input
    )
    {
        var result = new List<AiGeneratedImagePerson>();

        var existing = aiImageId == 0 ? [] : await repo.GetAiImagePerson(aiImageId).ToListAsync();
        var ordinal = 1;
        foreach (var i in input)
        {
            var entity = existing.FirstOrDefault(a => a.Id == i.Id) ?? repo.Add(new AiGeneratedImagePerson());
            map.Map(i, entity);

            var selfie = await repo.GetAiCharacterImage(i.ParticipantAiCharacterSelfieId).Include(a => a.Character).FirstOrDefaultAsync();

            if (selfie == null)
                throw AppErrorWithStatusCodeException.BadRequest(
                    $"Selfie with Id={i.ParticipantAiCharacterSelfieId} is not found",
                    "AiContent_SelfieIsNotFound"
                );

            entity.AiGeneratedImageId = aiImageId;
            entity.Ordinal = ordinal;
            entity.ParticipantGroupId = selfie.Character.GroupId;
            entity.GenderId = selfie.Character.GenderId;
            entity.Files = entity.Files.Length == 0 ? selfie.Files : entity.Files;

            result.Add(entity);

            ordinal++;
        }

        return result;
    }

    private async Task<List<AiGeneratedImageSource>> GetOrCreateAiImageSources(
        long aiImageId,
        IEnumerable<AiGeneratedImageSourceInput> input
    )
    {
        var result = new List<AiGeneratedImageSource>();

        var existing = aiImageId == 0 ? [] : await repo.GetAiImageSources(aiImageId).ToListAsync();

        foreach (var i in input)
        {
            var entity = existing.FirstOrDefault(a => a.Id == i.Id) ?? repo.Add(new AiGeneratedImageSource());
            map.Map(i, entity);

            entity.AiGeneratedImageId = aiImageId;

            result.Add(entity);
        }

        return result;
    }

    private async Task<AiGeneratedVideo> GetOrCreateAiVideo(AiGeneratedVideoInput input)
    {
        var entity = input.Id == 0
                         ? repo.Add(new AiGeneratedVideo {GroupId = currentUser})
                         : await repo.GetOwnAiVideoById(input.Id, currentUser).SingleAsync();

        map.Map(input, entity);

        return entity;
    }

    private async Task<AiGeneratedVideoClip> GetOrCreateAiVideoClip(long aiVideoId, AiGeneratedVideoClipInput input)
    {
        var entity = input.Id == 0
                         ? repo.Add(new AiGeneratedVideoClip())
                         : await repo.GetAiVideoClips(aiVideoId).SingleAsync(c => c.Id == input.Id);

        map.Map(input, entity);
        entity.AiGeneratedVideoId = aiVideoId;

        return entity;
    }

    private async Task<long> CopyImage(AiGeneratedImage image, IFileUploader uploader)
    {
        var newImage = ToImage(image);
        repo.Add(newImage);
        await repo.SaveChanges();

        var dbPersons = await repo.GetAiImagePerson(image.Id).AsNoTracking().ToListAsync();
        var newPersons = dbPersons.Select(i => ToImagePerson(image, i)).ToList();
        if (newPersons.Count > 0)
            repo.AddRange(newPersons);

        var dbSources = await repo.GetAiImageSources(image.Id).AsNoTracking().ToListAsync();
        var newSources = dbSources.Select(i => ToImageSource(image, i)).ToList();
        if (newSources.Count > 0)
            repo.AddRange(newSources);

        if (newSources.Count > 0 || newPersons.Count > 0)
            await repo.SaveChanges();

        await uploader.UploadFiles<AiGeneratedImage>(newImage);
        await uploader.UploadFilesAll<AiGeneratedImagePerson>(newPersons);
        await uploader.UploadFilesAll<AiGeneratedImageSource>(newSources);

        return newImage.Id;
    }

    private static AiGeneratedImage ToImage(AiGeneratedImage image)
    {
        return new AiGeneratedImage
               {
                   GroupId = image.GroupId,
                   NumOfCharacters = image.NumOfCharacters,
                   Seed = image.Seed,
                   Prompt = image.Prompt,
                   ShortPromptSummary = image.ShortPromptSummary,
                   AiMakeupId = image.AiMakeupId,
                   AiArtStyleId = image.AiArtStyleId,
                   Workflow = image.Workflow,
                   ModerationResult = image.ModerationResult,
                   IsModerationPassed = image.IsModerationPassed,
                   Files = CopyFiles(image.Files)
               };
    }

    private static AiGeneratedImageSource ToImageSource(AiGeneratedImage image, AiGeneratedImageSource i)
    {
        return new AiGeneratedImageSource {AiGeneratedImageId = image.Id, Type = i.Type, Files = CopyFiles(i.Files)};
    }

    private static AiGeneratedImagePerson ToImagePerson(AiGeneratedImage image, AiGeneratedImagePerson i)
    {
        return new AiGeneratedImagePerson
               {
                   AiGeneratedImageId = image.Id,
                   Ordinal = i.Ordinal,
                   ParticipantGroupId = i.ParticipantGroupId,
                   ParticipantAiCharacterSelfieId = i.ParticipantAiCharacterSelfieId,
                   GenderId = i.GenderId,
                   Files = CopyFiles(i.Files)
               };
    }
}