using System;
using System.Reflection;
using Amazon.MediaConvert;
using Amazon.S3;
using Amazon.SQS;
using AssetServer.Shared.AssetCopying;
using AssetStoragePathProviding;
using Common.Infrastructure.CloudFront;
using Common.Infrastructure.MusicProvider;
using Common.Models.Database.Interfaces;
using FluentValidation;
using Frever.AdminService.Core.JsonToModel;
using Frever.AdminService.Core.Services.AccountModeration;
using Frever.AdminService.Core.Services.AI;
using Frever.AdminService.Core.Services.AiContent;
using Frever.AdminService.Core.Services.AssetTransaction;
using Frever.AdminService.Core.Services.CreatePage;
using Frever.AdminService.Core.Services.EntityServices;
using Frever.AdminService.Core.Services.GeoClusters;
using Frever.AdminService.Core.Services.HashtagModeration;
using Frever.AdminService.Core.Services.HashtagModeration.DataAccess;
using Frever.AdminService.Core.Services.InAppPurchases;
using Frever.AdminService.Core.Services.Localizations;
using Frever.AdminService.Core.Services.ModelSettingsProviders;
using Frever.AdminService.Core.Services.MusicModeration;
using Frever.AdminService.Core.Services.MusicProvider;
using Frever.AdminService.Core.Services.ReadinessService;
using Frever.AdminService.Core.Services.RoleModeration;
using Frever.AdminService.Core.Services.StorageFiles;
using Frever.AdminService.Core.Services.UserActionSetting;
using Frever.AdminService.Core.Services.VideoModeration;
using Frever.AdminService.Core.Services.VideoModeration.DataAccess;
using Frever.AdminService.Core.Validation;
using Frever.Client.Core.Features.AI.UserGeneratedContent.Content;
using Frever.Client.Shared.AI.PixVerse;
using Frever.Shared.AssetStore;
using Frever.Shared.MainDb.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Frever.AdminService.Core.Installers;

// ReSharper disable once UnusedType.Global
// This class loaded and executed via reflection
public sealed class EntityServiceInstaller : IInstaller
{
    public void AddServices(IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddTrivialPerEntityService<IEntity>(
            typeof(IReadEntityService<>).GetTypeInfo(),
            typeof(DefaultReadEntityService<>).GetTypeInfo()
        );

        services.AddTrivialPerEntityService<IEntity>(
            typeof(IEntityReadAlgorithm<>).GetTypeInfo(),
            typeof(DefaultEntityReadAlgorithm<>).GetTypeInfo()
        );

        services.AddTrivialPerEntityService<IEntity>(
            typeof(IWriteEntityService<>).GetTypeInfo(),
            typeof(DefaultWriteEntityService<>).GetTypeInfo()
        );

        // Write algorithm
        services.AddScoped<IEntityWriteAlgorithm<Group>, GroupWriteAlgorithm>();
        services.AddScoped<IEntityWriteAlgorithm<ExternalSong>, ExternalSongWriteAlgorithm>();

        services.AddTrivialPerEntityService<IFileOwner>(typeof(IEntityWriteAlgorithm<>), typeof(FileOwnerWriteAlgorithm<>));
        services.AddTrivialPerEntityService<IEntity>(typeof(IEntityWriteAlgorithm<>), typeof(DefaultEntityWriteAlgorithm<>));

        services.AddScoped<AssetGroupProvider>();
        services.AddTrivialPerEntityService<IEntity>(typeof(IEntityLifeCycle<>), typeof(DefaultEntityLifeCycle<>));

        services.AddScoped<IEntityPartialUpdateService, EntityPartialUpdateService>();

        // Validators
        services.AddTrivialPerEntityService<IFileOwner>(typeof(IEntityValidator<>), typeof(FileOwnerValidator<>));
        services.AddMultiplePerEntityService<IEntity>(typeof(IEntityValidator<>), typeof(EntityValidator<>));

        var bucketName = configuration.GetValue<string>("AWS:bucket_name");
        var cloudFrontHost = configuration.GetValue<string>("CloudFrontHost");
        var ingestVideoBucket = configuration.GetValue<string>("IngestVideoS3BucketName");

        var assetCopyingOptions = new AssetCopyingOptions();
        configuration.Bind("AssetCopying", assetCopyingOptions);
        services.AddAssetCopying(assetCopyingOptions);

        var namingHelperOptions = new VideoNamingHelperOptions
                                  {
                                      DestinationVideoBucket = bucketName,
                                      CloudFrontHost = cloudFrontHost,
                                      IngestVideoBucket = ingestVideoBucket
                                  };
        services.AddSingleton(namingHelperOptions);
        services.AddSingleton<VideoNamingHelper>();

        var accountModerationServiceOptions = new AccountModerationServiceOptions {Bucket = bucketName};
        accountModerationServiceOptions.Validate();
        services.AddSingleton(accountModerationServiceOptions);

        services.AddCloudFrontConfiguration(configuration);

        services.AddScoped<IVideoRepository, PersistentVideoRepository>();
        services.AddScoped<IReportVideoRepository, PersistentReportVideoRepository>();
        services.AddScoped<IVideoModerationService, VideoModerationService>();
        services.AddScoped<IHashtagModerationService, HashtagModerationService>();
        services.AddScoped<IHashtagRepository, HashtagRepository>();
        services.AddScoped<IValidator<HashtagUpdate>, HashtagUpdateValidator>();

        var mediaFingerprintOptions2 = new Client.Core.Features.MediaFingerprinting.MediaFingerprintingOptions();
        configuration.GetSection("AcrCloud").Bind(mediaFingerprintOptions2);
        mediaFingerprintOptions2.LogBucket = bucketName;
        mediaFingerprintOptions2.Validate();
        services.AddAiGeneratedContent(mediaFingerprintOptions2);

        services.AddAiContentAdmin();
        services.AddAi();
        services.AddPixVerseProxy(configuration);
        services.AddAssetStoreTransactionAdmin();
        services.AddAssetStoreTransactions();
        services.AddCreatePage();
        services.AddGeoClusters();
        services.AddInAppPurchaseAdmin();
        services.AddLocalizations();
        services.AddRoleModeration();
        services.AddStorageFiles();
        services.AddUserActionSettings();
        services.AddReadiness();
        services.AddMusicProvider(configuration);
        services.AddMusicModeration();
        services.AddMusicProviderOAuth();
        services.AddMusicProviderOAuthSettings(configuration);
        services.AddMusicProviderApiSettings(configuration);

        services.AddScoped<IAccountModerationService, AccountModerationService>();
        services.AddScoped<IAccountHardDeletionService, AccountHardDeletionService>();
        services.AddScoped<IAccountModerationRepository, PersistentAccountModerationRepository>();
        services.AddScoped<HardDeleteAccountDataHelper>();
        services.AddOptions<HardDeleteAccountSettings>().Bind(configuration.GetSection(nameof(HardDeleteAccountSettings)));

        services.AddAWSService<IAmazonS3>();
        services.AddAWSService<IAmazonMediaConvert>();
        services.AddAWSService<IAmazonSQS>();

        services.AddHostedService<BackgroundServices.AccountHardDeletionService>();
    }
}