using System;
using AuthServer.Permissions;
using AuthServerShared;
using Common.Infrastructure.Database;
using Common.Infrastructure.EmailSending;
using Frever.Cache.Configuration;
using Frever.Client.Core.Features.Social.DataAccess;
using Frever.Client.Core.Features.Social.Followers;
using Frever.Client.Core.Features.Social.GroupBlocking;
using Frever.Client.Core.Features.Social.MyProfileInfo;
using Frever.Client.Core.Features.Social.Profiles;
using Frever.Client.Core.Features.Social.PublicProfiles;
using Frever.Client.Shared.Payouts;
using Frever.Client.Shared.Social;
using Frever.Shared.MainDb.Entities;
using Frever.Videos.Shared.MusicGeoFiltering;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NotificationService.Client;

namespace Frever.Client.Core.Features.Social;

public static class ServiceConfiguration
{
    public static void AddSocialServices(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddMusicLicenseFiltering(configuration);
        services.AddEmailSending(configuration);

        var profileServiceOptions = new ProfileServiceOptions();
        configuration.Bind(profileServiceOptions);
        profileServiceOptions.Validate();
        services.AddSingleton(profileServiceOptions);

        var appsFlyerSettings = new AppsFlyerSettings();
        configuration.Bind(nameof(AppsFlyerSettings), appsFlyerSettings);
        appsFlyerSettings.Validate();
        services.AddSingleton(appsFlyerSettings);

        var dbConnectionConfig = configuration.GetDbConnectionConfiguration();

        services.AddScoped<IMainDbRepository, EntityFrameworkMainDbRepository>();
        services.AddScoped<IFollowingService, FollowingService>();
        services.AddScoped<IProfileService, ProfileService>();
        services.AddScoped<IMyProfileService, MyProfileService>();
        services.AddScoped<IPhoneLookupService, DefaultPhoneLookupService>();
        services.AddScoped<IBlockUserService, BlockUserService>();
        services.AddSocialSharedService();
        services.AddPhoneNormalizationService();
        services.AddScoped<IPublicProfileService, PublicProfileService>();
        services.AddScoped<IAppsFlyerClient, HttpAppsFlyerClient>();

        services.AddSingleton<IFollowRecommendationClient, HttpFollowRecommendationClient>();

        services.AddNotificationServiceClient(configuration);

        services.AddFreverPermissionManagement(dbConnectionConfig);
        services.AddPayouts(configuration);

        services.AddFreverCaching(o => o.Redis.Hash<VideoKpi>());
    }
}