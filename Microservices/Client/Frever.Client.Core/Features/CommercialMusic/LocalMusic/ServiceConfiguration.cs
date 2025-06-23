using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Frever.Client.Core.Features.CommercialMusic;

public static class ServiceConfiguration
{
    public static void AddLocalMusicSearch(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Sergii: moved to cron job
        //services.AddHostedService<RefreshLocalMusicWorker>();
        services.AddScoped<IRefreshLocalMusicService, RefreshLocalMusicService>();
        services.AddScoped<IMusicSearchService, MusicSearchService>();

        var spotifyPopularitySettings = new SpotifyPopularitySettings();
        configuration.Bind("SpotifyPopularity", spotifyPopularitySettings);
        spotifyPopularitySettings.Validate();

        // Sergii: moved to cron job
        // services.AddHostedService<RefreshSpotifyPopularityWorker>();
        services.AddSingleton(spotifyPopularitySettings);
        services.AddScoped<ISpotifyPopularityService, SpotifyPopularityService>();
    }
}