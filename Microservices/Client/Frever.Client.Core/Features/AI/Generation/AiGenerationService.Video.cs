using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Common.Infrastructure;
using Common.Models;
using Frever.Client.Core.Features.AI.Generation.Contract;
using Frever.Client.Core.Features.AI.Generation.StatusUpdating;
using Frever.Client.Core.Features.AI.UserGeneratedContent.Content.Input;
using Frever.Client.Shared.AI.ComfyUi;
using Frever.Client.Shared.AI.ComfyUi.Contract;
using Frever.Client.Shared.AI.PixVerse;
using Frever.ClientService.Contract.Ai;
using Frever.Shared.MainDb.Entities;
using Microsoft.AspNetCore.Http;

namespace Frever.Client.Core.Features.AI.Generation;

public partial class AiGenerationService
{
    public async Task<ComfyUiResponse> PostVideoMusicGenGeneration(VideoMusicGenInput input)
    {
        await userPermissionService.EnsureCurrentUserActive();

        ArgumentNullException.ThrowIfNull(input);

        var info = await GetVideoInfoByContentId(input.AiGeneratedContentId, true);

        var message = new ComfyUiMusicGenMessage
                      {
                          InputS3Key = info.Path,
                          PromptText = input.PromptText,
                          BackGroundMusicContextValue = DetermineMusicGenContext(input.Context, info.Workflow)
                      };

        return await ProcessGeneration(
                   message,
                   new AiGenerationInput
                   {
                       Type = AiGeneratedContentType.Video,
                       Workflow = Workflows.Keys.MusicGen,
                       SourceContentId = input.AiGeneratedContentId,
                       Prompt = info.Prompt,
                       VideoLengthSec = info.Duration
                   }
               );
    }

    public async Task<ComfyUiResponse> PostVideoSfxGeneration(VideoMusicGenInput input)
    {
        await userPermissionService.EnsureCurrentUserActive();

        ArgumentNullException.ThrowIfNull(input);

        var info = await GetVideoInfoByContentId(input.AiGeneratedContentId, true);

        var message = new ComfyUiInputAndAudioAndPromptMessage
                      {
                          InputS3Key = info.Path,
                          PromptText = input.PromptText,
                          ContextValues =
                          [
                              (int) DetermineAudioAudioMode(input.AudioAudioMode, info.Workflow),
                              (int) (input.AudioPromptMode ?? AudioPromptMode.AutoPrompt)
                          ]
                      };

        return await ProcessGeneration(
                   message,
                   new AiGenerationInput
                   {
                       Type = AiGeneratedContentType.Video,
                       Workflow = Workflows.Keys.MmAudio,
                       SourceContentId = input.AiGeneratedContentId,
                       Prompt = info.Prompt,
                       VideoLengthSec = info.Duration
                   }
               );
    }

    public async Task<ComfyUiResponse> PostVideoLipSyncGeneration(VideoAudioAndPromptInput input)
    {
        await userPermissionService.EnsureCurrentUserActive();

        ArgumentNullException.ThrowIfNull(input);

        if (input.SongId == null && input.ExternalSongId == null && input.UserSoundId == null && input.PromptText == null)
            throw AppErrorWithStatusCodeException.BadRequest("SoundId is empty", ErrorCodes.Video.VideoNotFound);

        var info = await GetVideoInfoByContentId(input.AiGeneratedContentId);

        IComfyUiMessage message = input.PromptText != null
                                      ? new ComfyUiVideoAndAudioAndPromptMessage
                                        {
                                            VideoId = info.Id,
                                            VideoS3Key = info.Path,
                                            PromptText = input.PromptText,
                                            VideoDurationSeconds = info.Duration,
                                            ContextValues =
                                            [
                                                (await metadataService.GetSpeakerModeOrDefaultInternal(input.SpeakerModeId)).Id,
                                                (await metadataService.GetLanguageModeOrDefaultInternal(input.LanguageModeId)).Id
                                            ]
                                        }
                                      : new ComfyUiVideoLivePortraitMessage
                                        {
                                            InputS3Key = info.Path,
                                            SourceAudioS3Key =
                                                await soundService.GetSoundServerPath(
                                                    input.SongId,
                                                    input.ExternalSongId,
                                                    input.UserSoundId
                                                ),
                                            SourceAudioStartTime = input.ActivationCueSec,
                                            SourceAudioDuration = input.VideoDurationSec,
                                            LivePortraitAudioInputModeContextValue = (int) AudioInputMode.InputAudio,
                                            LivePortraitCopperModeContextValue = (int) CopperMode.IfCopperModeCuda,
                                            LivePortraitModelModeContextValue = (int) PortraitModelMode.Human
                                        };

        return await ProcessGeneration(
                   message,
                   new AiGenerationInput
                   {
                       Type = AiGeneratedContentType.Video,
                       Workflow = input.PromptText != null ? Workflows.Keys.LipSyncText : Workflows.Keys.LivePortrait,
                       Prompt = input.PromptText,
                       SourceContentId = input.AiGeneratedContentId,
                       ExternalSongId = input.ExternalSongId,
                       UserSoundId = input.UserSoundId,
                       SongId = input.SongId,
                       VideoLengthSec = input.PromptText != null ? info.Duration : input.VideoDurationSec
                   }
               );
    }

    public async Task<ComfyUiResponse> PostVideoOnOutputTransformation(VideoAudioAndPromptInput input)
    {
        await userPermissionService.EnsureCurrentUserActive();

        ArgumentNullException.ThrowIfNull(input);

        if (input.SongId == null && input.ExternalSongId == null && input.UserSoundId == null && input.PromptText == null)
            throw AppErrorWithStatusCodeException.BadRequest("SoundId is empty", ErrorCodes.Video.VideoNotFound);

        var info = await GetVideoInfoByContentId(input.AiGeneratedContentId, true);

        var message = new ComfyUiVideoAndAudioAndPromptMessage
                      {
                          VideoId = info.Id,
                          VideoS3Key = info.Path,
                          VideoDurationSeconds = info.Duration,
                          AudioStartTime = input.ActivationCueSec,
                          AudioDuration = input.VideoDurationSec
                      };

        if (input.PromptText == null)
        {
            message.AudioS3Key = await soundService.GetSoundServerPath(input.SongId, input.ExternalSongId, input.UserSoundId);
            message.AudioS3Bucket = namingHelper.VideoBucket;
        }
        else
        {
            message.PromptText = input.PromptText;
            message.ContextValues =
            [
                (await metadataService.GetSpeakerModeOrDefaultInternal(input.SpeakerModeId)).Id,
                (await metadataService.GetLanguageModeOrDefaultInternal(input.LanguageModeId)).Id
            ];
        }

        return await ProcessGeneration(
                   message,
                   new AiGenerationInput
                   {
                       Type = AiGeneratedContentType.Video,
                       Workflow = message.AudioS3Key == null ? Workflows.Keys.TextToVideo : Workflows.Keys.AudioToVideo,
                       Prompt = input.PromptText == null ? null : info.Prompt,
                       SourceContentId = input.AiGeneratedContentId,
                       ExternalSongId = input.ExternalSongId,
                       UserSoundId = input.UserSoundId,
                       SongId = input.SongId,
                       VideoLengthSec = info.Duration
                   }
               );
    }

    public async Task<PixVerseProgressResponse> PostVideoPixVerseGeneration(PixVerseInput input)
    {
        await userPermissionService.EnsureCurrentUserActive();

        var path = await GetImageInfoByContentId(input.AiGeneratedContentId);

        var s3Object = await s3.GetObjectAsync(namingHelper.VideoBucket, path);

        await using var memoryStream = new MemoryStream();
        await s3Object.ResponseStream.CopyToAsync(memoryStream);
        memoryStream.Position = 0;

        var fileInput = new PixVerseFileInput
                        {
                            Duration = input.Duration,
                            Prompt = input.Prompt,
                            File = new FormFile(
                                       memoryStream,
                                       0,
                                       memoryStream.Length,
                                       "file",
                                       path.Split("/").Last()
                                   ) {Headers = new HeaderDictionary(), ContentType = s3Object.Headers.ContentType}
                        };
        return await PostVideoPixVerseFromFileGeneration(fileInput, input.AiGeneratedContentId);
    }

    public async Task<PixVerseProgressResponse> PostVideoPixVerseFromFileGeneration(PixVerseFileInput input, long? sourceContentId = null)
    {
        await userPermissionService.EnsureCurrentUserActive();

        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(input.File);
        ArgumentException.ThrowIfNullOrWhiteSpace(input.Prompt);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(input.Duration);

        var id = await aiGeneratedContentService.SaveDraftInternal(
                     new AiGenerationInput
                     {
                         Type = AiGeneratedContentType.Video,
                         Workflow = PixVerseWorkflow,
                         Prompt = input.Prompt,
                         VideoLengthSec = input.Duration,
                         SourceContentId = sourceContentId
                     }
                 );

        var purchase = await PurchaseWorkflow(id, PixVerseWorkflow, null);
        if (purchase.ErrorMessage != null)
        {
            await RefundAndRemoveGeneratedContent(false, id);
            return new PixVerseProgressResponse {ErrorMessage = purchase.ErrorMessage, ErrorCode = purchase.ErrorCode};
        }

        var uploadImage = await pixVerseProxy.UploadImage(input.File);
        if (uploadImage.ErrorMessage != null)
        {
            await RefundAndRemoveGeneratedContent(purchase.IsPurchaseCharged, id);
            return new PixVerseProgressResponse {ErrorMessage = uploadImage.ErrorMessage, ErrorCode = uploadImage.ErrorCode};
        }

        var request = new PixVerseRequest {Duration = input.Duration, Prompt = input.Prompt, ImgId = long.Parse(uploadImage.ImageId)};
        var result = await pixVerseProxy.ImageToVideo(request.ToJson());
        if (result.Ok)
        {
            var p = new AiContentGenerationParameters {Type = PixVerseWorkflow, Workflow = PixVerseWorkflow, Message = request.ToJson()};
            await aiGeneratedContentService.SetGenerationInfo(id, result.VideoId, p);
            await pollingManager.EnqueueJobAsync(id, PollingJob.PixVerse, result.VideoId, currentUser);
        }
        else
        {
            await RefundAndRemoveGeneratedContent(purchase.IsPurchaseCharged, id);
        }

        result.AiContentId = id;
        return result;
    }

    private static int DetermineMusicGenContext(MusicGenContext? context, string workflow)
    {
        if (workflow == PixVerseWorkflow)
            return (int) MusicGenContext.MuteIncomingAudio;
        if (Workflows.WithNarrationWorkflows.Contains(workflow))
            return (int) MusicGenContext.NarrationInput;
        if (Workflows.WithAudioWorkflows.Contains(workflow))
            return (int) (context ?? MusicGenContext.MuteIncomingAudio);
        return (int) MusicGenContext.MuteIncomingAudio;
    }

    private static AudioAudioMode DetermineAudioAudioMode(AudioAudioMode? context, string workflow)
    {
        if (workflow == PixVerseWorkflow)
            return AudioAudioMode.MuteIncomingAudioBackgroundMusicNoVoices;

        if (Workflows.WithNarrationWorkflows.Contains(workflow))
            return context is AudioAudioMode.NarrationInputBackgroundMusicNoVoices or AudioAudioMode.NarrationInputBackgroundMusicWithVoices
                       ? context.Value
                       : AudioAudioMode.NarrationInputBackgroundMusicNoVoices;

        if (Workflows.WithAudioWorkflows.Contains(workflow))
            return context ?? AudioAudioMode.MuteIncomingAudioBackgroundMusicNoVoices;

        if (context != AudioAudioMode.MuteIncomingAudioBackgroundMusicNoVoices &&
            context != AudioAudioMode.MuteIncomingAudioBackgroundMusicWithVoices)
            return AudioAudioMode.MuteIncomingAudioBackgroundMusicNoVoices;

        return context.Value;
    }
}