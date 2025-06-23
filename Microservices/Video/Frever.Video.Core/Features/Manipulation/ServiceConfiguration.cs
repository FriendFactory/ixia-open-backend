using System;
using Frever.Video.Core.Features.Manipulation.DataAccess;
using Microsoft.Extensions.DependencyInjection;

namespace Frever.Video.Core.Features.Manipulation;

public static class ServiceConfiguration
{
    public static void AddVideoManipulation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IVideoManipulationRepository, PersistentVideoManipulationRepository>();
        services.AddScoped<IVideoManipulationService, VideoManipulationService>();
    }
}