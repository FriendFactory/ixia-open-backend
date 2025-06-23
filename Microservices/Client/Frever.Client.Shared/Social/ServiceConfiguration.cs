using System;
using Frever.Client.Shared.Files;
using Frever.Client.Shared.Social.DataAccess;
using Frever.Client.Shared.Social.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Frever.Client.Shared.Social;

public static class ServiceConfiguration
{
    public static void AddSocialSharedService(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IMainRepository, MainRepository>();
        services.AddScoped<ISocialSharedService, SocialSharedService>();

        services.AddEntityFiles();
        services.AddEntityFileConfiguration<GroupFileConfig>();
    }
}