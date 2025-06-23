using System;
using Frever.AdminService.Core.Services.Social.DataAccess;
using Microsoft.Extensions.DependencyInjection;

namespace Frever.AdminService.Core.Services.Social;

public static class ServiceConfiguration
{
    public static void AddSocialManagement(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<ISocialAdminMainDbRepository, SocialAdminMainDbRepository>();
        services.AddScoped<IProfileService, ProfileService>();
    }
}