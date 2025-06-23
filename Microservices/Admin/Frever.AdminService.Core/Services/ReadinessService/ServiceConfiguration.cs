using FluentValidation;
using Frever.AdminService.Core.Services.ReadinessService.DataAccess;
using Microsoft.Extensions.DependencyInjection;

namespace Frever.AdminService.Core.Services.ReadinessService;

public static class ServiceConfiguration
{
    public static void AddReadiness(this IServiceCollection services)
    {
        services.AddScoped<IReadinessRepository, PersistentReadinessRepository>();
        services.AddScoped<IReadinessService, ReadinessService>();
        services.AddScoped<IValidator<ReadinessInfo>, ReadinessInfoValidator>();
    }
}