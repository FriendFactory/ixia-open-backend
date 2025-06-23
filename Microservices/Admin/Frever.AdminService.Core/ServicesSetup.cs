using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Frever.AdminService.Core.Installers;
using Frever.AdminService.Core.Services.DeviceBlacklist;
using Frever.AdminService.Core.UoW;
using Frever.Cache.Configuration;
using Frever.Videos.Shared.GeoClusters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

[assembly: InternalsVisibleTo("Server.Tests")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

namespace Frever.AdminService.Core;

public static class ServicesSetup
{
    public static void AddFreverServices(this IServiceCollection services, IConfiguration configuration)
    {
        var installersTypes = typeof(ServicesSetup).Assembly.GetTypes()
                                                   .Where(x => typeof(IInstaller).IsAssignableFrom(x) && !x.IsAbstract && !x.IsInterface)
                                                   .ToArray();
        var allInstallers = installersTypes.Select(Activator.CreateInstance).Cast<IInstaller>().ToArray();
        foreach (var installer in allInstallers)
            installer.AddServices(services, configuration);

        services.AddScoped<IUnitOfWork, EntityFrameworkUnitOfWork>();

        services.AddFreverCaching(_ => { });
        services.AddGeoCluster();
        services.AddDeviceBlacklistAdmin();
    }
}