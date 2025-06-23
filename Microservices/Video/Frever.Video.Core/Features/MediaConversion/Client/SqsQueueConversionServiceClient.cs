using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.SQS;
using Frever.Video.Contract.Messages;
using Frever.Video.Core.Features.Shared;
using Newtonsoft.Json;

#pragma warning disable CS8625

namespace Frever.Video.Core.Features.MediaConversion.Client;

internal sealed class SqsQueueConversionServiceClient(VideoServerOptions config, IAmazonSQS sqs) : IMediaConvertServiceClient
{
    private readonly VideoServerOptions _config = config ?? throw new ArgumentNullException(nameof(config));
    private readonly IAmazonSQS _sqs = sqs ?? throw new ArgumentNullException(nameof(sqs));

    public async Task CreateMp3ExtractionJob(string sourceBucketPath, string destinationBucketPath)
    {
        var message = new CreateConversionJobMessage
                      {
                          VideoId = -1,
                          RoleArn = _config.ConvertJobRoleArn,
                          UserMetadata = new Dictionary<string, string>(),
                          DestinationBucketPath = destinationBucketPath,
                          JobTemplateName = _config.TranscodingJobTemplateName,
                          SourceBucketPath = sourceBucketPath,
                          Queue = _config.ExtractAudioQueue,
                          HasLandscapeOrientation = false
                      };

        await _sqs.SendMessageAsync(_config.ConversionJobSqsQueue, JsonConvert.SerializeObject(message));
    }

    public async Task CreateVideoConversionJob(
        long videoId,
        string sourceBucketPath,
        string destinationBucketPath,
        bool hasLandscapeOrientation = false
    )
    {
        await AddConversionJob(
            videoId,
            sourceBucketPath,
            destinationBucketPath,
            VideoConversionType.Video,
            _config.ConvertJobTemplateName,
            hasLandscapeOrientation
        );

        await AddConversionJob(
            videoId,
            sourceBucketPath,
            destinationBucketPath,
            VideoConversionType.Thumbnail,
            _config.VideoThumbnailJobTemplateName,
            hasLandscapeOrientation
        );
    }

    private async Task AddConversionJob(
        long videoId,
        string sourceBucketPath,
        string destinationBucketPath,
        VideoConversionType conversionType,
        string jobTemplateName,
        bool hasLandscapeOrientation = false
    )
    {
        var metadata = ConversionJobMetadataHelper.CreateMetadata(videoId, conversionType);

        var message = new CreateConversionJobMessage
                      {
                          VideoId = videoId,
                          RoleArn = _config.ConvertJobRoleArn,
                          UserMetadata = metadata,
                          DestinationBucketPath = destinationBucketPath,
                          JobTemplateName = jobTemplateName,
                          SourceBucketPath = sourceBucketPath,
                          Queue = _config.MediaConverterQueue,
                          HasLandscapeOrientation = hasLandscapeOrientation
                      };

        await _sqs.SendMessageAsync(_config.ConversionJobSqsQueue, JsonConvert.SerializeObject(message));
    }
}