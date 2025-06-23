using System;
using Amazon.MediaConvert;
using Amazon.SQS;
using Frever.Cache.Configuration;
using Frever.Cache.Strategies;
using Frever.Video.Core.Features.MediaConversion.Client;
using Frever.Video.Core.Features.MediaConversion.DataAccess;
using Frever.Video.Core.Features.MediaConversion.Mp3Extraction;
using Frever.Video.Core.Features.MediaConversion.StatusUpdating;
using Microsoft.Extensions.DependencyInjection;

namespace Frever.Video.Core.Features.MediaConversion;

public static class ServiceConfiguration
{
    public static void AddMediaConversion(this IServiceCollection services, string mediaConverterQueue)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (string.IsNullOrWhiteSpace(mediaConverterQueue))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(mediaConverterQueue));

        services.AddAWSService<IAmazonSQS>();
        services.AddAWSService<IAmazonMediaConvert>();

        services.AddScoped<IVideoConversionStatusUpdateService, VideoConversionStatusUpdateService>();
        services.AddSingleton(new JobPollingStatusUpdaterConfiguration {MediaConvertQueue = mediaConverterQueue});
        services.AddHostedService<JobPollingVideoConversionStatusUpdater>();

        services.AddSingleton<IMediaConvertServiceClient, SqsQueueConversionServiceClient>();

        services.AddScoped<IVideoToMp3TranscodingService, MediaConvertVideoToMp3TranscodingService>();
        services.AddScoped<IVideoStatusUpdateRepository, PersistentVideoStatusUpdateRepository>();

        services.AddFreverCaching(
            builder =>
            {
                builder.Redis.Dictionary<long, AwsMediaConvertJobCacheInfo>(
                    JobPollingVideoConversionStatusUpdater.MediaConvertJobStatusRedisKey(),
                    SerializeAs.Json
                );
            }
        );
    }
}