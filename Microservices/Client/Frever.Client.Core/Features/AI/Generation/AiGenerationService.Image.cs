using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Common.Infrastructure;
using Frever.Client.Core.Features.AI.Generation.Contract;
using Frever.Client.Core.Features.AI.UserGeneratedContent.Content.Input;
using Frever.Client.Shared.AI.ComfyUi;
using Frever.Client.Shared.AI.ComfyUi.Contract;
using Frever.Client.Shared.Files;
using Frever.ClientService.Contract.Ai;

namespace Frever.Client.Core.Features.AI.Generation;

public partial class AiGenerationService
{
    private static readonly Regex UrlRegex = new(@"https://([^\.]+)\.s3\.[^\/]+\/(.+?)(?:\?|$)", RegexOptions.IgnoreCase);

    public async Task<ComfyUiResponse> PostTextToImageGeneration(ImageGenerationInput input, string key)
    {
        await userPermissionService.EnsureCurrentUserActive();

        ArgumentException.ThrowIfNullOrWhiteSpace(input.PromptText);

        var message = new ComfyUiPhotoAndPromptMessage {PromptText = input.PromptText};

        return await ProcessGeneration(
                   message,
                   new AiGenerationInput
                   {
                       Type = AiGeneratedContentType.Image,
                       Workflow = Workflows.Keys.Flux,
                       Prompt = input.PromptText,
                       CharacterImageIds = input.CharacterImageIds
                   },
                   key ?? WorkflowKey.ImageFromPrompt
               );
    }

    public async Task<ComfyUiResponse> PostImageToImageGeneration(ImageGenerationInput input, string key)
    {
        await userPermissionService.EnsureCurrentUserActive();

        ArgumentNullException.ThrowIfNull(input);

        if (input.FileUrls == null || input.FileUrls.Count == 0 || input.FileUrls.Count > 3)
            throw new ArgumentOutOfRangeException(nameof(input.FileUrls));

        var message = new ComfyUiMultiPhotoAndPromptMessage
                      {
                          PromptText = input.PromptText,
                          InputS3Key = GetFilePathFromUrl(input.FileUrls[FileKeys.Input1]),
                          SourceS3Keys = input.FileUrls.Where(e => e.Key != FileKeys.Input1)
                                              .Select(e => GetFilePathFromUrl(e.Value))
                                              .ToList()
                      };

        var data = input.FileUrls.Count switch
                   {
                       1 => (Workflows.Keys.FluxPhoto, WorkflowKey.ImageFromPromptAnd1Image),
                       2 => (Workflows.Keys.PulidTwoChars, WorkflowKey.ImageFromPromptAnd2Images),
                       3 => (Workflows.Keys.PulidThreeChars, WorkflowKey.ImageFromPromptAnd3Images),
                       _ => throw new ArgumentOutOfRangeException()
                   };

        return await ProcessGeneration(
                   message,
                   new AiGenerationInput
                   {
                       Type = AiGeneratedContentType.Image,
                       Workflow = data.Item1,
                       Prompt = input.PromptText,
                       CharacterImageIds = input.CharacterImageIds
                   },
                   key ?? data.Item2
               );
    }

    public async Task<ComfyUiResponse> PostImageStyleGeneration(ImageGenerationInput input)
    {
        await userPermissionService.EnsureCurrentUserActive();

        ArgumentNullException.ThrowIfNull(input);

        var inputFile = input.FileUrls?.GetValueOrDefault(FileKeys.Target);
        if (input.AiGeneratedContentId == null && inputFile == null)
            throw AppErrorWithStatusCodeException.BadRequest("Either file or key required", "INPUT_PARAMETERS_WRONG");

        var message = new ComfyUiMultiPhotoAndPromptMessage
                      {
                          SourceS3Keys = [GetFilePathFromUrl(input.FileUrls?.GetValueOrDefault(FileKeys.Style))],
                          InputS3Key = input.AiGeneratedContentId.HasValue
                                           ? await GetImageInfoByContentId(input.AiGeneratedContentId.Value)
                                           : GetFilePathFromUrl(inputFile)
                      };

        return await ProcessGeneration(
                   message,
                   new AiGenerationInput
                   {
                       Type = AiGeneratedContentType.Image,
                       Workflow = Workflows.Keys.FluxPhotoRedux,
                       Prompt = input.PromptText,
                       CharacterImageIds = input.CharacterImageIds,
                       SourceContentId = input.AiGeneratedContentId
                   }
               );
    }

    public async Task<ComfyUiResponse> PostImageMakeUpGeneration(long id, ImageGenerationInput input)
    {
        await userPermissionService.EnsureCurrentUserActive();

        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(input.AiGeneratedContentId);

        var makeUp = await metadataService.GetMakeUpByIdInternal(id);
        if (makeUp == null)
            throw AppErrorWithStatusCodeException.NotFound("Make up not found", "MAKE_UP_NOT_FOUND");

        if (!Workflows.PhotoWorkflows.Contains(makeUp.Category.Workflow))
            throw AppErrorWithStatusCodeException.NotFound("Workflow not found", "WORKFLOW_NOT_FOUND");

        var message = new ComfyUiMultiPhotoAndPromptMessage
                      {
                          SourceS3Keys = [makeUp.Files.Main()?.Path],
                          InputS3Key = await GetImageInfoByContentId(input.AiGeneratedContentId!.Value)
                      };

        return await ProcessGeneration(
                   message,
                   new AiGenerationInput
                   {
                       Type = AiGeneratedContentType.Image,
                       Workflow = makeUp.Category.Workflow,
                       Prompt = input.PromptText,
                       CharacterImageIds = input.CharacterImageIds,
                       SourceContentId = input.AiGeneratedContentId,
                       AiMakeupId = makeUp.Id
                   }
               );
    }

    public async Task<ComfyUiResponse> PostImageWardrobeGeneration(ImageGenerationInput input)
    {
        await userPermissionService.EnsureCurrentUserActive();

        ArgumentNullException.ThrowIfNull(input);

        var inputFile = input.FileUrls?.GetValueOrDefault(FileKeys.Target);
        if (input.AiGeneratedContentId == null && inputFile == null)
            throw AppErrorWithStatusCodeException.BadRequest("Either file or key required", "INPUT_PARAMETERS_WRONG");

        if (input.WardrobeModeId != null && !Enum.IsDefined(typeof(WardrobeMode), (int) input.WardrobeModeId.Value))
            throw new ArgumentException(nameof(input.WardrobeModeId));

        var wardrobeMode = input.WardrobeModeId == null ? WardrobeMode.FullClothes : (WardrobeMode) input.WardrobeModeId;

        var message = new ComfyUiPhotoAcePlusMessage
                      {
                          SourceS3Key = GetFilePathFromUrl(input.FileUrls?.GetValueOrDefault(FileKeys.Outfit)),
                          AcePlusWardrobeModeContextValue = (int) wardrobeMode,
                          AcePlusReferenceModeContextValue = (int) ReferenceMode.UploadImage,
                          AcePlusMaskModeContextValue = (int) MaskMode.Auto,
                          InputS3Key = input.AiGeneratedContentId.HasValue
                                           ? await GetImageInfoByContentId(input.AiGeneratedContentId.Value)
                                           : GetFilePathFromUrl(inputFile)
                      };

        return await ProcessGeneration(
                   message,
                   new AiGenerationInput
                   {
                       Type = AiGeneratedContentType.Image,
                       Workflow = Workflows.Keys.AcePlus,
                       Prompt = input.PromptText,
                       CharacterImageIds = input.CharacterImageIds,
                       SourceContentId = input.AiGeneratedContentId
                   }
               );
    }

    public async Task<ComfyUiResponse> PostImageLipSyncGeneration(ImageAudioAndPromptInput input)
    {
        await userPermissionService.EnsureCurrentUserActive();

        ArgumentNullException.ThrowIfNull(input);

        var inputFile = input.FileUrls?.GetValueOrDefault(FileKeys.Target);
        if (input.AiGeneratedContentId == 0 && inputFile == null)
            throw AppErrorWithStatusCodeException.BadRequest("Either file or key required", "INPUT_PARAMETERS_WRONG");

        if (input.SongId == null && input.ExternalSongId == null && input.UserSoundId == null && input.PromptText == null)
            throw AppErrorWithStatusCodeException.BadRequest("Either prompt or audio required", "INPUT_PARAMETERS_WRONG");

        var message = new ComfyUiInputAndAudioAndPromptMessage
                      {
                          PromptText = input.PromptText,
                          AudioStartTime = input.ActivationCueSec,
                          AudioDuration = input.VideoDurationSec,
                          InputS3Key = input.AiGeneratedContentId.HasValue
                                           ? await GetImageInfoByContentId(input.AiGeneratedContentId.Value)
                                           : GetFilePathFromUrl(inputFile)
                      };

        if (input.PromptText == null)
            message.AudioS3Key = await soundService.GetSoundServerPath(input.SongId, input.ExternalSongId, input.UserSoundId);
        else
            message.ContextValues =
            [
                (await metadataService.GetSpeakerModeOrDefaultInternal(input.SpeakerModeId)).Id,
                (await metadataService.GetLanguageModeOrDefaultInternal(input.LanguageModeId)).Id
            ];

        return await ProcessGeneration(
                   message,
                   new AiGenerationInput
                   {
                       Type = AiGeneratedContentType.Video,
                       Workflow = message.AudioS3Key == null ? Workflows.Keys.SonicText : Workflows.Keys.SonicAudio,
                       Prompt = input.PromptText,
                       SourceContentId = input.AiGeneratedContentId,
                       VideoLengthSec = input.VideoDurationSec,
                       ExternalSongId = input.ExternalSongId,
                       UserSoundId = input.UserSoundId,
                       SongId = input.SongId
                   }
               );
    }

    public async Task<ComfyUiResponse> PostImageBackgroundAudioGeneration(ImageAudioAndPromptInput input)
    {
        await userPermissionService.EnsureCurrentUserActive();

        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(input.AiGeneratedContentId);

        if (input.SongId == null && input.ExternalSongId == null && input.UserSoundId == null && input.PromptText == null)
            throw AppErrorWithStatusCodeException.BadRequest("Either prompt or audio required", "INPUT_PARAMETERS_WRONG");

        var path = await GetImageInfoByContentId(input.AiGeneratedContentId.Value);
        var message = new ComfyUiInputAndAudioAndPromptMessage
                      {
                          InputS3Key = path,
                          PromptText = input.PromptText,
                          AudioStartTime = input.ActivationCueSec,
                          AudioDuration = input.VideoDurationSec
                      };

        if (input.PromptText == null)
            message.AudioS3Key = await soundService.GetSoundServerPath(input.SongId, input.ExternalSongId, input.UserSoundId);
        else
            message.ContextValues =
            [
                (await metadataService.GetSpeakerModeOrDefaultInternal(input.SpeakerModeId)).Id,
                (await metadataService.GetLanguageModeOrDefaultInternal(input.LanguageModeId)).Id
            ];

        return await ProcessGeneration(
                   message,
                   new AiGenerationInput
                   {
                       Type = AiGeneratedContentType.Video,
                       Workflow = message.AudioS3Key == null ? Workflows.Keys.StillImageText : Workflows.Keys.StillImageAudio,
                       Prompt = input.PromptText,
                       SourceContentId = input.AiGeneratedContentId,
                       ExternalSongId = input.ExternalSongId,
                       UserSoundId = input.UserSoundId,
                       SongId = input.SongId
                   }
               );
    }
}