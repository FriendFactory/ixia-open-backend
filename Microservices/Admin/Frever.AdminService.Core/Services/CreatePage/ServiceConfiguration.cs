using System;
using Microsoft.Extensions.DependencyInjection;

namespace Frever.AdminService.Core.Services.CreatePage;

public static class ServiceConfiguration
{
    public static void AddCreatePage(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<ICreatePageService, CreatePageService>();
    }
}