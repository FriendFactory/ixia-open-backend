using System;
using Microsoft.Extensions.DependencyInjection;

namespace Frever.AdminService.Core.Services.Localizations;

public static class ServiceConfiguration
{
    public static void AddLocalizations(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<ILocalizationModerationService, LocalizationModerationService>();
    }
}