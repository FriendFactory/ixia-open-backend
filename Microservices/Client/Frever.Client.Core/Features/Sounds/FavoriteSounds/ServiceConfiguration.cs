using System;
using Microsoft.Extensions.DependencyInjection;

namespace Frever.Client.Core.Features.Sounds.FavoriteSounds;

public static class ServiceConfiguration
{
    public static void AddFavoriteSounds(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IFavoriteSoundRepository, FavoriteSoundRepository>();
        services.AddScoped<IFavoriteSoundService, FavoriteSoundService>();
    }
}