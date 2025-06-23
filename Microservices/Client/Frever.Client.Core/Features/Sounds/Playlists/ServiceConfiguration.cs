using System;
using Frever.Cache.Configuration;
using Frever.Cache.Strategies;
using Microsoft.Extensions.DependencyInjection;

namespace Frever.Client.Core.Features.Sounds.Playlists;

public static class ServiceConfiguration
{
    public static void AddExternalPlaylists(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IExternalPlaylistRepository, ExternalPlaylistRepository>();
        services.AddScoped<IExternalPlaylistService, ExternalPlaylistService>();

        services.AddFreverCaching(
            options =>
            {
                options.InMemory.Blob<ExternalPlaylistModel[]>(
                    SerializeAs.Protobuf,
                    false,
                    typeof(Frever.Shared.MainDb.Entities.ExternalPlaylist)
                );
            }
        );
    }
}