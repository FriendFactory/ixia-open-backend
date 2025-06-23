using System;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Frever.AdminService.Core.Services.GeoClusters;

public static class ServiceConfiguration
{
    public static void AddGeoClusters(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IGeoClusterService, GeoClusterService>();
        services.AddScoped<IValidator<GeoClusterDto>, GeoClusterValidator>();
    }
}