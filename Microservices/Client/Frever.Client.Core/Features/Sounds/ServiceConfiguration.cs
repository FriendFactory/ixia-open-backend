using System;
using Frever.Client.Core.Features.Sounds.FavoriteSounds;
using Frever.Client.Core.Features.Sounds.Song;
using Frever.Client.Core.Features.Sounds.UserSounds;
using Microsoft.Extensions.DependencyInjection;

namespace Frever.Client.Core.Features.Sounds;

public static class ServiceConfiguration
{
    public static void AddSounds(this IServiceCollection services, IAssetServerSettings settings)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton(settings);

        services.AddUserSoundAsset();
        services.AddSongAsset();
        services.AddFavoriteSounds();
    }
}