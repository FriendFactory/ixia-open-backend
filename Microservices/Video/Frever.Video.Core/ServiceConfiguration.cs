using System;
using AssetServer.Shared.AssetCopying;
using AssetStoragePathProviding;
using Common.Infrastructure.CloudFront;
using Common.Infrastructure.Messaging;
using Common.Infrastructure.ModerationProvider;
using Frever.Client.Core.Features.AI.UserGeneratedContent.Content;
using Frever.Client.Core.Features.AI.UserGeneratedContent.Content.Core.Mapping;
using Frever.Client.Shared.ActivityRecording;
using Frever.Client.Shared.AI.ComfyUi;
using Frever.Video.Core.Features;
using Frever.Video.Core.Features.AssetUrlGeneration;
using Frever.Video.Core.Features.Caching;
using Frever.Video.Core.Features.Comments;
using Frever.Video.Core.Features.CreatePage;
using Frever.Video.Core.Features.Feeds;
using Frever.Video.Core.Features.Hashtags;
using Frever.Video.Core.Features.Manipulation;
using Frever.Video.Core.Features.MediaConversion;
using Frever.Video.Core.Features.MusicProvider;
using Frever.Video.Core.Features.PersonalFeed;
using Frever.Video.Core.Features.ReportInappropriate;
using Frever.Video.Core.Features.Shared;
using Frever.Video.Core.Features.Sharing;
using Frever.Video.Core.Features.Uploading;
using Frever.Video.Core.Features.VideoInformation;
using Frever.Video.Core.Features.Views;
using Frever.Videos.Shared.CachedVideoKpis;
using Frever.Videos.Shared.GeoClusters;
using Frever.Videos.Shared.MusicGeoFiltering;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NotificationService.Client;

namespace Frever.Video.Core;

public static class ServiceConfiguration
{
    public static void AddVideoServices(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var bucketName = configuration.GetValue<string>("AWS:bucket_name");

        var options = new VideoServerOptions();
        configuration.Bind(options);
        options.Validate();
        services.AddSingleton(options);

        var videoNamingHelperOptions = new VideoNamingHelperOptions
        {
            DestinationVideoBucket = bucketName,
            CloudFrontHost = options.CloudFrontHost,
            IngestVideoBucket = options.IngestVideoS3BucketName
        };
        configuration.Bind(videoNamingHelperOptions);
        videoNamingHelperOptions.Validate();
        services.AddSingleton(videoNamingHelperOptions);
        services.AddSingleton<VideoNamingHelper>();

        var assetCopyingOptions = new AssetCopyingOptions();
        configuration.Bind("AssetCopying", assetCopyingOptions);
        services.AddAssetCopying(assetCopyingOptions);

        services.AddCloudFrontConfiguration(configuration);

        services.AddModerationProviderApi(configuration);
        services.AddSnsMessaging(configuration);

        services.AddVideoComments();
        services.AddMediaConversion(options.MediaConverterQueue);
        services.AddVideoViewsFeatures(bucketName);
        services.AddHashtagStatsUpdating();
        services.AddVideoSharedFeatures();
        services.AddVideoAssetUrlGeneration();
        services.AddVideoInfoLoading();
        services.AddVideoManipulation();
        services.AddVideoSharing();
        services.AddInappropriateVideoReporting();
        services.AddVideoUploading();
        services.AddVideoFeeds();
        services.AddMusicProvider(configuration);
        services.AddCreatePage();

        var mediaFingerprintOptions2 = new Client.Core.Features.MediaFingerprinting.MediaFingerprintingOptions();
        configuration.GetSection("AcrCloud").Bind(mediaFingerprintOptions2);
        mediaFingerprintOptions2.LogBucket = bucketName;
        mediaFingerprintOptions2.Validate();
        services.AddAiGeneratedContent(mediaFingerprintOptions2);

        services.AddScoped<IVideoCachingService, RedisVideoCachingService>();

        services.AddAutoMapper(typeof(ServiceConfiguration), typeof(ReadingAiGeneratedContentMappingProfile));

        services.AddCachedVideoKpis();
        services.AddGeoCluster();
        services.AddComfyUiApi(configuration);
        services.AddPersonalFeed(configuration);
        services.AddMusicLicenseFiltering(configuration);
        services.AddNotificationServiceClient(configuration);
        services.AddUserActivityRecording(configuration);
    }
}