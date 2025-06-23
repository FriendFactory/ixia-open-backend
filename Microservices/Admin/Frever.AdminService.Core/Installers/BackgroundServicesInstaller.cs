using System;
using Frever.AdminService.Core.BackgroundServices;
using Microsoft.Extensions.DependencyInjection;

namespace Frever.AdminService.Core.Installers;

public static class BackgroundServicesInstaller
{
    public static void AddBackgroundServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddHostedService<AccountHardDeletionService>();
        services.AddHostedService<StarCreatorCandidateService>();
    }
}