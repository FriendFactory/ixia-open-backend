using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using AssetStoragePathProviding;
using AuthServer.Permissions.Services;
using AuthServerShared;
using Common.Infrastructure;
using Common.Infrastructure.Caching;
using Common.Infrastructure.EnvironmentInfo;
using Common.Models;
using Frever.Client.Core.Features.AI.Generation.Contract;
using Frever.Client.Core.Features.AI.Generation.StatusUpdating;
using Frever.Client.Core.Features.AI.Metadata;
using Frever.Client.Core.Features.AI.UserGeneratedContent.Content;
using Frever.Client.Core.Features.AI.UserGeneratedContent.Content.Input;
using Frever.Client.Shared.AI.Billing;
using Frever.Client.Shared.AI.ComfyUi;
using Frever.Client.Shared.AI.ComfyUi.Contract;
using Frever.Client.Shared.AI.PixVerse;
using Frever.Client.Shared.Files;
using Frever.Shared.MainDb.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Frever.Client.Core.Features.AI.Generation;

public partial class AiGenerationService(
    ICache cache,
    IAmazonS3 s3,
    UserInfo currentUser,
    IComfyUiClient comfyUiClient,
    IUserPermissionService userPermissionService,
    ISoundService soundService,
    IAiMetadataService metadataService,
    IGenerationRepository repo,
    IAiBillingService billingService,
    IPixVerseProxy pixVerseProxy,
    IAiGeneratedContentService aiGeneratedContentService,
    VideoNamingHelper namingHelper,
    EnvironmentInfo environmentInfo,
    IPollingJobManager pollingManager,
    ILogger<AiGenerationService> logger
) : IAiGenerationService
{
    private const string ComfyUiType = "comfy-ui";
    private const string PixVerseWorkflow = "pix-verse";

    public async Task<GenerationUrlDto> GetGenerationResult(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        await userPermissionService.EnsureCurrentUserActive();

        var response = await comfyUiClient.GetResult(key, currentUser);
        if (response == null || response.S3Key == "not_found" || response.MainKey == "not_found")
            return new GenerationUrlDto {ErrorMessage = "Error getting transformation result"};

        await cache.Put(key, response, TimeSpan.FromMinutes(15));

        var result = new GenerationUrlDto
                     {
                         Ok = true,
                         Workflow = response.Workflow,
                         MainFileUrl = await GetSignedUrl(response.S3Bucket, response.S3Key ?? response.MainKey)
                     };

        if (response.CoverKey != null)
            result.CoverFileUrl = await GetSignedUrl(response.S3Bucket, response.CoverKey);

        if (response.ThumbnailKey != null)
            result.ThumbnailFileUrl = await GetSignedUrl(response.S3Bucket, response.ThumbnailKey);

        if (response.MaskKey != null)
            result.MaskFileUrl = await GetSignedUrl(response.S3Bucket, response.MaskKey);

        return result;

        Task<string> GetSignedUrl(string s3Bucket, string s3Key)
        {
            return s3.GetPreSignedURLAsync(
                new GetPreSignedUrlRequest {BucketName = s3Bucket, Key = s3Key, Expires = DateTime.Now.AddMinutes(5)}
            );
        }
    }

    private async Task<ComfyUiResponse> ProcessGeneration(IComfyUiMessage message, AiGenerationInput input, string key = null)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(input);

        message.Enrich(environmentInfo.Type, namingHelper.VideoBucket, currentUser.UserMainGroupId);

        var hideResult = key is WorkflowKey.CharacterFromPrompt or WorkflowKey.CharacterFromImageAndPrompt;
        var id = await aiGeneratedContentService.SaveDraftInternal(input, hideResult);

        var purchase = await PurchaseWorkflow(id, input.Workflow, key);
        if (purchase.ErrorMessage != null)
        {
            await RefundAndRemoveGeneratedContent(false, id);
            return ComfyUiResponse.Failed(purchase.ErrorMessage, purchase.ErrorCode);
        }

        var result = await comfyUiClient.PostGeneration(input.Workflow, message);
        result.AiContentId = id;

        if (result.IsSuccess)
            await aiGeneratedContentService.SetGenerationInfo(
                id,
                message.ToResultKey(input.Workflow),
                new AiContentGenerationParameters {Type = ComfyUiType, Workflow = input.Workflow, Message = message.ToJson()}
            );
        else
            await RefundAndRemoveGeneratedContent(purchase.IsPurchaseCharged, id);

        return result;
    }

    private async Task<(bool IsPurchaseCharged, string ErrorMessage, string ErrorCode)> PurchaseWorkflow(
        long aiContentId,
        string workflow,
        string key
    )
    {
        try
        {
            var result = await billingService.TryPurchaseAiWorkflowRun(workflow, key, aiContentId, null);
            logger.LogInformation("Purchased Workflow={Wf} run", PixVerseWorkflow);
            return (result, null, null);
        }
        catch (AiWorkflowPurchaseNotEnoughBalanceException ex)
        {
            return (false, ex.Message, ex.ErrorCode);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Error purchasing Workflow={Wf}, Key={K}, ContentId={Id}",
                workflow,
                key,
                aiContentId
            );
            return (false, "Error purchasing workflow run", null);
        }
    }

    private async Task RefundAndRemoveGeneratedContent(bool isPurchaseCharged, long id)
    {
        if (isPurchaseCharged)
            await billingService.RefundAiWorkflowRun(id);
        await aiGeneratedContentService.Delete(id);
    }

    private async Task<VideoShortInfo> GetVideoInfoByContentId(long contentId, bool withPrompt = false)
    {
        var video = await repo.GetAiGeneratedVideoById(contentId, currentUser);
        if (video == null)
            throw AppErrorWithStatusCodeException.NotFound("Video not fount", ErrorCodes.Video.VideoNotFound);

        var mainFile = video.Files.Main();
        if (mainFile?.Path == null)
            throw AppErrorWithStatusCodeException.NotFound("Video not fount", ErrorCodes.Video.VideoNotFound);

        var prompt = withPrompt ? await repo.GetAiGeneratedVideoClip(video.Id).Select(e => e.Prompt).FirstOrDefaultAsync() : null;

        return new VideoShortInfo(
            video.Id,
            video.LengthSec,
            mainFile.Path,
            prompt,
            video.Workflow
        );
    }

    private async Task<string> GetImageInfoByContentId(long contentId)
    {
        var image = await repo.GetAiGeneratedImageById(contentId, currentUser);
        if (image == null)
            throw AppErrorWithStatusCodeException.BadRequest("Image does not exist", "IMAGE_NOT_FOUND");

        var mainFile = image.Files.Main();
        if (mainFile?.Path == null)
            throw AppErrorWithStatusCodeException.BadRequest("Image does not exist", "IMAGE_NOT_FOUND");

        return mainFile.Path;
    }

    private static string GetFilePathFromUrl(string url)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);

        var match = UrlRegex.Match(url);
        if (!match.Success)
            throw new ArgumentException("Invalid file url", nameof(url));

        var path = match.Groups[2].Value;

        var extension = Path.GetExtension(path)?.TrimStart('.');
        if (!ComfyUiClient.AllowedFormats.Contains(extension, StringComparer.OrdinalIgnoreCase))
            throw new ArgumentException("Invalid file url", nameof(url));

        return path;
    }

    private static class FileKeys
    {
        public const string Input1 = "input1";
        public const string Input2 = "input2";
        public const string Input3 = "input3";
        public const string Target = "target";
        public const string Style = "style";
        public const string Outfit = "outfit";
    }

    private record VideoShortInfo(
        long Id,
        int Duration,
        string Path,
        string Prompt,
        string Workflow
    );
}