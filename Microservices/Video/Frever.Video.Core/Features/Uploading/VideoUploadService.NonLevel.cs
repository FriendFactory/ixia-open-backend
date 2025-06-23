using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Common.Infrastructure;
using Common.Infrastructure.Videos;
using Common.Models;
using FluentValidation;
using Frever.Client.Shared.Files;
using Frever.ClientService.Contract.Ai;
using Frever.Shared.MainDb.Entities;
using Frever.Shared.MainDb.Extensions;
using Frever.Video.Core.Features.AssetUrlGeneration;
using Frever.Video.Core.Features.Uploading.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Frever.Video.Core.Features.Uploading;

internal sealed partial class VideoUploadService
{
    public async Task<Frever.Shared.MainDb.Entities.Video> CompleteNonLevelVideoUploading(
        string uploadId,
        CompleteNonLevelVideoUploadingRequest request
    )
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrEmpty(uploadId);

        log.LogInformation("Complete non-level video upload {UploadId} by group {GroupId}", uploadId, currentUser.UserMainGroupId);

        await nonLevelVideoUploadValidator.ValidateAndThrowAsync(request);
        await userPermissionService.EnsureCurrentUserActive();
        await parentalConsent.EnsureVideoUploadAllowed();

        if (!await userPermissionService.IsCurrentUserStarCreator())
            request.Links = null;

        var uploadVideoS3Key = namingHelper.GetUploadVideoS3Key(currentUser, uploadId);
        var s3File = await s3.GetObjectAsync(namingHelper.SourceVideoBucket, uploadVideoS3Key);
        using var ms = new MemoryStream();
        await s3File.ResponseStream.CopyToAsync(ms);
        var buffer = ms.ToArray();

        log.LogInformation("Checking S3 file for copyrighted content at {Path}", namingHelper.GetUploadVideoS3Path(currentUser, uploadId));

        try
        {
            var moderationResult = await moderationProviderApi.CallModerationProviderApi(buffer, request.Format, uploadVideoS3Key);
            if (!moderationResult.PassedModeration)
            {
                log.LogError(
                    "Video uploaded doesn't pass moderation: {Reason} {Error}",
                    moderationResult.Reason,
                    moderationResult.ErrorMessage
                );
                throw AppErrorWithStatusCodeException.BadRequest(moderationResult.Reason, "Video content is inappropriate.");
            }
        }
        catch (Exception ex)
        {
            if (ex is AppErrorWithStatusCodeException)
                throw;
            log.LogError(ex, "Failed to call moderation API");
        }

        var video = await CreateNonLevelVideoRecord(request, request.AiContentId);

        await CreateVideoConversionJob(uploadId, video, request);

        return video;
    }

    public async Task<Frever.Shared.MainDb.Entities.Video> PublishAiContent(CompleteNonLevelVideoUploadingRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        await nonLevelVideoUploadValidator.ValidateAndThrowAsync(request);
        await userPermissionService.EnsureCurrentUserActive();

        log.LogInformation(
            "Complete AI video upload for AI Content ID={id} by group {GroupId}",
            request.AiContentId,
            currentUser.UserMainGroupId
        );

        ArgumentNullException.ThrowIfNull(request.AiContentId);

        if (!await userPermissionService.IsCurrentUserStarCreator())
            request.Links = null;

        var aiContent = await aiGeneratedContentService.Publish(request.AiContentId.Value);
        var video = await CreateNonLevelVideoRecord(request, aiContent.Id);

        if (aiContent.Type == AiGeneratedContentType.Video && aiContent.Video != null)
        {
            log.LogInformation("Publishing AI Video");
            var aiVideo = await repo.GetAiVideo(aiContent.Video.Id).FirstOrDefaultAsync();
            if (aiVideo != null)
            {
                var mainFile = aiVideo.Files.Main();
                if (mainFile != null && !string.IsNullOrWhiteSpace(mainFile.Path))
                {
                    log.LogInformation("Creating AI video conversion job for video {path}", mainFile.Path);
                    await CreateVideoConversionForAiVideo(mainFile.Path, video, request);
                }
                else
                {
                    log.LogError("AI Video main file is not found or invalid");
                    throw AppErrorWithStatusCodeException.BadRequest(
                        "AI Video main file is not found or invalid",
                        "PUBLISH_AI_VIDEO_NO_MAIN_FILE"
                    );
                }
            }
            else
            {
                log.LogError("AI Content {id} is not video or doesn't contain video information", aiContent.Id);
                throw AppErrorWithStatusCodeException.BadRequest(
                    "AI Content is not a video or doesn't have a video ID",
                    "PUBLISH_AI_VIDEO_AI_CONTENT_VIDEO_INVALID"
                );
            }
        }
        else
        {
            log.LogInformation("AI Image publishing requested, skipping conversion job creation");
        }

        return video;
    }

    private async Task<Frever.Shared.MainDb.Entities.Video> CreateNonLevelVideoRecord(
        VideoUploadingRequestBase request,
        long? aiContentId = null
    )
    {
        var language = await repo.GetGroupLanguage(currentUser).FirstOrDefaultAsync();
        var country = await repo.GetGroupCountry(currentUser).FirstOrDefaultAsync();

        var location = await locationProvider.Get();

        var newVideo = new Frever.Shared.MainDb.Entities.Video
                       {
                           Duration = request.DurationSec,
                           Size = request.Size,
                           FrameRate = 29,
                           GroupId = currentUser,
                           LevelId = null,
                           Watermark = false,
                           ResolutionHeight = 1920,
                           ResolutionWidth = 1080,
                           CharactersCount = 0,
                           RemixedFromVideoId = null,
                           Version = Guid.NewGuid().ToString("N"),
                           IsDeleted = true,
                           IsRemixable = false,
                           TemplateIds = [],
                           Description = request.Description,
                           Access = request.Access,
                           ConversionStatus = VideoConversion.Started,
                           Language = language?.IsoCode.ToLower(),
                           Country = country?.ISOName.ToLower(),
                           Links = CloudFrontVideoAssetUrlGenerator.NormalizeLinks(request.Links),
                           ExternalSongIds = [],
                           SongInfo = [],
                           UserSoundInfo = [],
                           PublishTypeId =
                               request.PublishTypeId == KnownVideoTypes.VideoMessageId
                                   ? KnownVideoTypes.VideoMessageId
                                   : KnownVideoTypes.StandardId,
                           AllowComment = request.AllowComment,
                           AllowRemix = request.AllowRemix,
                           Location = Geo.FromLatLon(location.Lat, location.Lon),
                           UniverseId = 1,
                           RaceIds = [],
                           AiContentId = aiContentId
                       };

        var taggedGroups = await taggingGroup.GetGroupsCanBeTagged(currentUser, request.TaggedFriendIds);
        var groupTags = taggedGroups.Where(e => e != currentUser && e != Constants.PublicAccessGroupId)
                                    .Select(e => new VideoGroupTag {GroupId = e})
                                    .ToArray();

        var video = await repo.CreateOrReplaceVideoAsync(newVideo, request.Hashtags, request.Mentions, groupTags);

        return video;
    }
}