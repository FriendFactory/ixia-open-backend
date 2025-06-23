using System;
using System.Threading.Tasks;
using Common.Infrastructure.Caching.CacheKeys;
using Common.Models.Files;
using Frever.Client.Shared.AI.Billing;
using Frever.Client.Shared.AI.ComfyUi;
using Frever.Client.Shared.AI.PixVerse;
using Frever.Client.Shared.Files;
using Frever.Shared.MainDb.Entities;
using Microsoft.Extensions.Logging;
using NotificationService;
using NotificationService.Client.Messages;
using StackExchange.Redis;

namespace Frever.Client.Core.Features.AI.Generation.StatusUpdating;

public interface IAiGeneratedContentUploadingService
{
    Task<PollingJobStatus> TryUploadGeneratedContent(long contentId, string contentType, string key, long groupId);
    Task SetContentGenerationFailed(long contentId);
}

public class AiGeneratedContentUploadingService(
    IConnectionMultiplexer redis,
    IComfyUiClient comfyUiClient,
    IPixVerseProxy pixVerseProxy,
    IGeneratedContentUploadingRepository repository,
    IAiBillingService billingService,
    IFileStorageService fileStorage,
    INotificationAddingService notificationService,
    ILogger<AiGeneratedContentUploadingService> logger
) : IAiGeneratedContentUploadingService, IComfyUiMessageHandlingService
{
    private readonly IDatabase _db = redis.GetDatabase();
    private readonly IFileUploader _fileUploader = fileStorage.CreateFileUploader();

    public async Task SetContentGenerationFailed(long contentId)
    {
        logger.LogWarning("Setting upload as failed for content ID: {ContentId}", contentId);

        var content = await repository.GetAiGeneratedContentById(contentId);
        if (content == null)
        {
            logger.LogError("Content not found for content ID: {ContentId}", contentId);
            return;
        }

        if (content.GenerationStatus != AiGeneratedContent.KnownGenerationStatusInProgress)
        {
            logger.LogError("Content already has {Status} generation status", content.GenerationStatus);
            return;
        }

        await HandleFailedUpload(content);
    }

    public async Task UpdateGeneratedContentStatus(
        string partialName,
        string bucket,
        string mainSource,
        string thumbnailSource,
        string error
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(partialName);

        var content = await repository.GetAiGeneratedContentByPartialName(partialName);
        if (content == null)
        {
            logger.LogError("AiGeneratedContent not found by partial name {PartialName}", partialName);
            return;
        }

        if (content.GenerationStatus != AiGeneratedContent.KnownGenerationStatusInProgress)
        {
            logger.LogError("Content already has {Status} generation status", content.GenerationStatus);
            return;
        }

        if (error != null)
        {
            logger.LogError("Generation failed with error {Error}", error);
            await HandleFailedUpload(content);
            return;
        }

        await FinalizeSuccessfulUpload(
            content,
            content.AiGeneratedImageId.HasValue ? PollingJob.Image : PollingJob.Video,
            StorageReference.Encode(mainSource, bucket),
            string.IsNullOrWhiteSpace(thumbnailSource) ? null : StorageReference.Encode(thumbnailSource, bucket)
        );
    }

    public async Task<PollingJobStatus> TryUploadGeneratedContent(long contentId, string contentType, string key, long groupId)
    {
        var result = await PollGenerationResult(contentType, key, groupId);

        if (result.Status == PollingJobStatus.InProgress)
            return result.Status;

        var content = await repository.GetAiGeneratedContentById(contentId);
        if (content.GenerationStatus != AiGeneratedContent.KnownGenerationStatusInProgress)
        {
            logger.LogError("Content already has {Status} generation status", content.GenerationStatus);
            return result.Status;
        }

        if (result.Status == PollingJobStatus.Failed)
        {
            await HandleFailedUpload(content);
            return result.Status;
        }

        await FinalizeSuccessfulUpload(content, contentType, result.MainSource, result.ThumbnailSource);
        return result.Status;
    }

    private async Task<GenerationResult> PollGenerationResult(string contentType, string key, long groupId)
    {
        return contentType == PollingJob.PixVerse ? await GetPixVerseResult(key) : await GetComfyUiResult(key, groupId);
    }

    private async Task<GenerationResult> GetComfyUiResult(string key, long groupId)
    {
        var response = await comfyUiClient.GetResult(key, groupId);
        if (response == null)
        {
            logger.LogWarning("ComfyUI response is null for Key: {Key}", key);
            return new GenerationResult(PollingJobStatus.Failed);
        }

        if (response.S3Key == "not_found" || response.MainKey == "not_found")
            return new GenerationResult(PollingJobStatus.InProgress);

        return new GenerationResult(
            PollingJobStatus.Completed,
            StorageReference.Encode(response.S3Key ?? response.MainKey, response.S3Bucket),
            response.ThumbnailKey != null ? StorageReference.Encode(response.ThumbnailKey, response.S3Bucket) : null
        );
    }

    private async Task<GenerationResult> GetPixVerseResult(string key)
    {
        var result = await pixVerseProxy.GetResult(key);
        if (result.Ok && result.IsReady)
            return new GenerationResult(PollingJobStatus.Completed, result.Url);

        if (result.Ok && !result.IsReady)
            return new GenerationResult(PollingJobStatus.InProgress);

        logger.LogWarning("PixVerse result failed for Key: {Key}", key);
        return new GenerationResult(PollingJobStatus.Failed);
    }

    private async Task HandleFailedUpload(AiGeneratedContent content)
    {
        content.DeletedAt = DateTime.UtcNow;
        content.GenerationStatus = AiGeneratedContent.KnownGenerationStatusFailed;
        await repository.SaveChanges();

        await billingService.RefundAiWorkflowRun(content.Id);
        await _db.KeyDeleteAsync(AiContentCacheKeys.DraftGenerationStatusKey(content.Id));
    }

    private async Task FinalizeSuccessfulUpload(AiGeneratedContent content, string contentType, string mainSource, string thumbnailSource)
    {
        logger.LogInformation("Finalizing upload for Content Id: {Id}", content.Id);

        await using var transaction = await repository.BeginTransaction();

        content.GenerationStatus = AiGeneratedContent.KnownGenerationStatusCompleted;

        if (contentType == PollingJob.Image)
            await AddImageFiles(content.Id, content.GroupId, mainSource, thumbnailSource);
        else
            await AddVideoFiles(content.Id, content.GroupId, mainSource);

        await repository.SaveChanges();
        await transaction.Commit();

        await _fileUploader.WaitForCompletion();

        await _db.KeyDeleteAsync(AiContentCacheKeys.DraftGenerationStatusKey(content.Id));
        await notificationService.NotifyAiContentGenerated(
            new NotifyAiContentGeneratedMessage {AiContentId = content.Id, CurrentGroupId = content.GroupId}
        );
    }

    private async Task AddVideoFiles(long contentId, long groupId, string mainFileSource)
    {
        var video = await repository.GetAiGeneratedVideoById(contentId, groupId);
        video.Files =
        [
            new FileMetadata {Type = "main", Source = new FileSourceInfo {SourceFile = mainFileSource}}
        ];

        var clips = await repository.GetAiGeneratedVideoClips(video.Id);
        foreach (var clip in clips)
            clip.Files =
            [
                new FileMetadata {Type = "main", Source = new FileSourceInfo {SourceFile = mainFileSource}}
            ];
        await _fileUploader.UploadFiles<AiGeneratedVideo>(video);
        await _fileUploader.UploadFilesAll<AiGeneratedVideoClip>(clips);
        await repository.SaveChanges();
    }

    private async Task AddImageFiles(long contentId, long groupId, string mainSource, string thumbnailSource)
    {
        var image = await repository.GetAiGeneratedImageById(contentId, groupId);
        image.Files =
        [
            new FileMetadata {Type = "main", Source = new FileSourceInfo {SourceFile = mainSource}},
            new FileMetadata {Type = "thumbnail128", Source = new FileSourceInfo {SourceFile = thumbnailSource}}
        ];
        await _fileUploader.UploadFiles<AiGeneratedImage>(image);
        await repository.SaveChanges();
    }

    private record GenerationResult(PollingJobStatus Status, string MainSource = null, string ThumbnailSource = null);
}