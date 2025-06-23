using System;
using Frever.AdminService.Core.Services.UserActionSetting.DataAccess;
using Microsoft.Extensions.DependencyInjection;

namespace Frever.AdminService.Core.Services.UserActionSetting;

public static class ServiceConfiguration
{
    public static void AddUserActionSettings(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IUserActionSettingRepository, UserActionSettingRepository>();
        services.AddScoped<IUserActionSettingService, UserActionSettingService>();
    }
}