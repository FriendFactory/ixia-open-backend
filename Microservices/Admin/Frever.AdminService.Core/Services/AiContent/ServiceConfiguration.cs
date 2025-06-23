using System;
using Frever.AdminService.Core.Services.AiContent.DataAccess;
using Microsoft.Extensions.DependencyInjection;

namespace Frever.AdminService.Core.Services.AiContent;

public static class ServiceConfiguration
{
    public static void AddAiContentAdmin(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IAiContentRepository, PersistentAiContentRepository>();
        services.AddScoped<IAiContentAdminService, AiContentAdminService>();
    }
}