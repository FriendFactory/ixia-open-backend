using System;
using Common.Infrastructure.RequestId;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Frever.AdminService.Core.Installers;

internal sealed class ExternalServicesInstaller : IInstaller
{
    public void AddServices(IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddRequestIdAccessor();
        services.AddHttpClient();

        services.AddAutoMapper(typeof(ExternalServicesInstaller));
    }
}