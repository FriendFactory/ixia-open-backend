using System.Net;
using AssetStoragePathProviding;
using Common.Infrastructure.CloudFront;
using Common.Infrastructure.EnvironmentInfo;
using Common.Infrastructure.Messaging;
using Common.Infrastructure.ModerationProvider;
using Frever.Client.Core.Features.AI.Generation.StatusUpdating;
using Frever.Client.Core.Features.AI.Moderation.External.OpenAi;
using Frever.Client.Core.Features.AI.UserGeneratedContent.Content;
using Frever.Client.Core.Features.AppStoreApi;
using Frever.Client.Core.Features.InAppPurchases;
using Frever.Client.Core.Features.Localizations;
using Frever.Client.Core.Features.Social;
using Frever.Client.Core.Features.Sounds;
using Frever.Client.Core.Features.Sounds.FavoriteSounds;
using Frever.Client.Core.Features.Sounds.UserSounds;
using Frever.Client.Shared.ActivityRecording;
using Frever.Client.Shared.AI.Billing;
using Frever.Client.Shared.Files;
using Frever.Client.Shared.Payouts;
using Frever.Client.Shared.Social;
using Frever.Common.IntegrationTesting;
using Frever.Shared.MainDb.Entities;
using Frever.Video.Core;
using Frever.Video.Core.Features;
using Frever.Video.Core.Features.Manipulation;
using Frever.Videos.Shared.GeoClusters;
using Frever.Videos.Shared.MusicGeoFiltering.AbstractApi;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit.Abstractions;

namespace Frever.Client.Core.IntegrationTest.Utils;

public static class TestServiceConfiguration
{
    public static void AddClientIntegrationTests(this IServiceCollection services, ITestOutputHelper testOut)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(testOut);

        var configuration = IntegrationTestServiceConfiguration.GetConfiguration();
        var envInfo = configuration.BindEnvironmentInfo();
        services.AddSingleton(envInfo);
        services.AddSnsMessaging(configuration);

        var assetServerSettings = new AssetServerSettings();
        configuration.Bind("AssetServerSettings", assetServerSettings);
        services.AddSounds(assetServerSettings);

        services.AddAssetBucketPathService();

        AddModerationProviderApi(services);

        services.AddGeoCluster();
        services.AddPayouts(configuration);
        services.AddUserActivityRecording(configuration);
        services.AddSocialServices(configuration);
        services.AddLocalizations();
        services.AddInAppPurchasesCore(
            new InAppPurchaseOptions
            {
                IsProduction = false,
                OfferKeySecret = "ABC",
                GoogleApiKeyBase64 = "4455",
                PlayMarketPackageName = "com.frever.client-android",
                AppStoreBundleIdPrefix = "com.frever.client-ios"
            }
        );
        services.AddUserSoundAsset();
        services.AddFavoriteSounds();
        services.AddAiGeneratedContent(null);
        services.AddAiBilling(configuration);

        var appStoreApiOptions = new AppStoreApiOptions
                                 {
                                     IssuerId = "fake",
                                     KeyId = "fake",
                                     SharedSecret = "fake",
                                     KeyDataBase64 = "fake_base64"
                                 };
        configuration.Bind("AppStoreApi", appStoreApiOptions);
        services.AddAppStoreApi(appStoreApiOptions);

        AddModerationProviderApi(services);

        var videoServerOptions = new VideoServerOptions();
        configuration.Bind(videoServerOptions);

        services.AddSocialSharedService();
        services.AddCloudFrontConfiguration(configuration);
        services.AddVideoServices(configuration);

        services.AddAutoMapper(typeof(IVideoManipulationService), typeof(SoundMappingProfile));

        AddCurrentUserLocationProvider(services);
        AddSnsMessaging(services);
        AddEntityFilesMocks(services);
        AddPollingJobs(services);

        // This should be last to overwrite IReadDb/IWriteDb registration from AddXXX business methods.
        services.AddIntegrationTests(testOut);
        AddOpenAiModerationClient(services);
    }

    private static void AddOpenAiModerationClient(IServiceCollection services)
    {
        var openAiClientMock = new Mock<IOpenAiClient>();

        openAiClientMock.Setup(s => s.Moderate(It.IsAny<TextInput>(), It.IsAny<ImageUrlInput>()))
                        .ReturnsAsync(
                             new OpenAiModerationResponse
                             {
                                 Id = $"test-moderation-{Guid.NewGuid():N}",
                                 Model = "mock-model",
                                 Results =
                                 [
                                     new OpenAiModerationResult
                                     {
                                         Flagged = false,
                                         Categories = [],
                                         CategoryScores = [],
                                         CategoryAppliedInputTypes = []
                                     }
                                 ]
                             }
                         );


        var openAiDeps = services.Where(d => d.ServiceType == typeof(IOpenAiClient)).ToArray();
        foreach (var sd in openAiDeps)
            services.Remove(sd);

        services.AddScoped(_ => openAiClientMock.Object);
        services.AddSingleton(openAiClientMock);
    }

    private static void AddModerationProviderApi(IServiceCollection services)
    {
        var moderationProviderApi = new Mock<IModerationProviderApi>();
        services.AddSingleton(moderationProviderApi);
        services.AddSingleton(moderationProviderApi.Object);

        var ok = new ModerationResult {PassedModeration = true, StatusCode = 200};

        moderationProviderApi.Setup(s => s.CallModerationProviderApi(It.IsAny<IFormFile>())).ReturnsAsync(ok);
        moderationProviderApi.Setup(s => s.CallModerationProviderApi(It.IsAny<string>())).ReturnsAsync(ok);
        moderationProviderApi.Setup(s => s.CallModerationProviderApi(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>()))
                             .ReturnsAsync(ok);

        moderationProviderApi.Setup(s => s.CallModerationProviderApiText(It.IsAny<string>())).ReturnsAsync(ok);
    }

    private static void AddCurrentUserLocationProvider(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var mock = new Mock<IIpAddressProvider>();
        mock.Setup(s => s.GetIpAddressOfConnectedClient()).Returns(new IPAddress([146, 70, 162, 114]));

        services.AddSingleton(mock);
        services.AddSingleton(mock.Object);
    }

    private static void AddSnsMessaging(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var messagingService = new Mock<ISnsMessagingService>();
        services.AddSingleton(messagingService);
        services.AddSingleton(messagingService.Object);
    }

    private static void AddEntityFilesMocks(this IServiceCollection services)
    {
        services.AddEntityFiles();

        var fileStorageBackend = new Mock<IFileStorageBackend>();
        fileStorageBackend.Setup(s => s.MakeCdnUrl(It.IsAny<string>(), It.IsAny<bool>()))
                          .Returns((string url, bool _) => $"https://test.cdn.com/{url}");

        services.AddScoped(_ => fileStorageBackend.Object);
        services.AddSingleton(fileStorageBackend);
    }

    private static void AddPollingJobs(this IServiceCollection serviceCollection)
    {
        var pollingJob = new Mock<IPollingJobManager>();
        serviceCollection.AddSingleton(pollingJob);
        serviceCollection.AddScoped(_ => pollingJob.Object);
    }
}