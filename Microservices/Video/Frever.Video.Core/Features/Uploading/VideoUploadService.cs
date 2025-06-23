using System;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using AssetStoragePathProviding;
using AuthServer.Permissions.Services;
using AuthServer.Permissions.Sub13;
using AuthServerShared;
using Common.Infrastructure.Caching;
using Common.Infrastructure.Caching.CacheKeys;
using Common.Infrastructure.ModerationProvider;
using Common.Infrastructure.Utils;
using Common.Models;
using FluentValidation;
using Frever.Client.Core.Features.AI.UserGeneratedContent.Content;
using Frever.Video.Contract;
using Frever.Video.Core.Features.MediaConversion.Client;
using Frever.Video.Core.Features.Shared;
using Frever.Video.Core.Features.Uploading.DataAccess;
using Frever.Video.Core.Features.Uploading.Models;
using Frever.Videos.Shared.MusicGeoFiltering;
using Microsoft.Extensions.Logging;

namespace Frever.Video.Core.Features.Uploading;

internal sealed partial class VideoUploadService(
    IAmazonS3 s3,
    IMediaConvertServiceClient mediaConvert,
    VideoServerOptions config,
    VideoNamingHelper namingHelper,
    ILogger<VideoUploadService> log,
    UserInfo currentUser,
    IUserPermissionService userPermissionService,
    ICache cache,
    IModerationProviderApi moderationProviderApi,
    IValidator<CompleteNonLevelVideoUploadingRequest> nonLevelVideoUploadValidator,
    ICurrentLocationProvider locationProvider,
    IParentalConsentValidationService parentalConsent,
    IVideoUploadingRepository repo,
    ITaggingGroupProvider taggingGroup,
    IAiGeneratedContentService aiGeneratedContentService
) : IVideoUploadService
{
    public async Task<VideoUploadInfo> CreateVideoUpload()
    {
        await userPermissionService.EnsureCurrentUserActive();

        log.LogInformation("Init video uploading by user {MainGroupId}", currentUser.UserMainGroupId);

        log.LogTrace("Video upload request is valid");

        var uploadId = Guid.NewGuid().ToString();
        var s3Key = namingHelper.GetUploadVideoS3Key(currentUser, uploadId);
        log.LogTrace("New upload {UploadId} created successfully. Path to temp file to upload is {S3Key}", uploadId, s3Key);

        var uploadUrl = s3.GetPreSignedURL(
            new GetPreSignedUrlRequest
            {
                BucketName = config.IngestVideoS3BucketName,
                Key = s3Key,
                Verb = HttpVerb.PUT,
                Expires = DateTime.Now.AddMinutes(15)
            }
        );

        log.LogTrace("Signed URL for uploading is {UploadUrl}", uploadUrl);

        return new VideoUploadInfo(uploadId, uploadUrl);
    }

    private async Task CreateVideoConversionJob(string uploadId, IVideoNameSource video, VideoUploadingRequestBase request)
    {
        log.LogTrace("Validate uploaded file for upload {UploadId}", uploadId);
        var sourceBucketPath = namingHelper.GetUploadVideoS3Path(video.GroupId, uploadId);

        log.LogTrace("Moving temp upload file to input file");
        var destinationBucketPath = UriUtils.CombineUri(namingHelper.GetVideoS3Path(video), "video");
        log.LogTrace(
            "Create conversion job for {SourceBucketPath}, put result to {DestinationBucketPath}",
            sourceBucketPath,
            destinationBucketPath
        );

        await mediaConvert.CreateVideoConversionJob(
            video.Id,
            sourceBucketPath,
            destinationBucketPath,
            request.VideoOrientation == VideoOrientation.Landscape
        );

        cache.Db().KeyDelete(VideoCacheKeys.VideoJobWatchingSuspendedKey());
    }

    private async Task CreateVideoConversionForAiVideo(string aiVideoPath, IVideoNameSource video, VideoUploadingRequestBase request)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(aiVideoPath);

        var sourceBucketPath = namingHelper.GetDestS3Path(aiVideoPath);
        var destinationBucketPath = UriUtils.CombineUri(namingHelper.GetVideoS3Path(video), "video");

        log.LogTrace(
            "Create AI conversion job for {SourceBucketPath}, put result to {DestinationBucketPath}",
            sourceBucketPath,
            destinationBucketPath
        );

        await mediaConvert.CreateVideoConversionJob(
            video.Id,
            sourceBucketPath,
            destinationBucketPath,
            request.VideoOrientation == VideoOrientation.Landscape
        );

        cache.Db().KeyDelete(VideoCacheKeys.VideoJobWatchingSuspendedKey());
    }
}