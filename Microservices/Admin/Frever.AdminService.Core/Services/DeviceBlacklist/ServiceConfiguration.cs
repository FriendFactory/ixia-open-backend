using System;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Frever.AdminService.Core.Services.DeviceBlacklist;

public static class ServiceConfiguration
{
    public static void AddDeviceBlacklistAdmin(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IDeviceBlacklistAdminService, DeviceBlacklistAdminService>();
        services.AddScoped<IValidator<BlockDeviceParams>, BlockDeviceParamsValidator>();
    }
}