using Microsoft.Extensions.DependencyInjection;

namespace Frever.AdminService.Core.Services.RoleModeration;

public static class ServiceConfiguration
{
    public static void AddRoleModeration(this IServiceCollection services)
    {
        services.AddScoped<IRoleModerationService, RoleModerationService>();
    }
}