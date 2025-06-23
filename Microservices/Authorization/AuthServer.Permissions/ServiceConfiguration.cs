using System;
using AuthServer.Permissions.DataAccess;
using AuthServer.Permissions.Services;
using AuthServer.Permissions.Sub13;
using Common.Infrastructure.Database;
using Frever.Shared.MainDb;
using Microsoft.Extensions.DependencyInjection;

namespace AuthServer.Permissions;

public static class ServiceConfiguration
{
    public static void AddFreverPermissions(this IServiceCollection services, DatabaseConnectionConfiguration dbConnectionConfiguration)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddFreverCachedDataAccess(dbConnectionConfiguration);
        services.AddFreverDataWritingAccess(dbConnectionConfiguration);

        services.AddScoped<IMainGroupRepository, EntityFrameworkMainGroupRepository>();
        services.AddScoped<IUserPermissionService, FreverUserPermissionService>();
        services.AddScoped<IMinorUserService, MinorUserService>();
        services.AddScoped<IParentalConsentValidationService, DbParentalConsentValidationService>();
    }

    public static void AddFreverPermissionManagement(
        this IServiceCollection services,
        DatabaseConnectionConfiguration dbConnectionConfiguration
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(dbConnectionConfiguration);

        services.AddFreverPermissions(dbConnectionConfiguration);
        services.AddScoped<IUserPermissionManagementService, FreverUserPermissionManagementService>();
    }
}