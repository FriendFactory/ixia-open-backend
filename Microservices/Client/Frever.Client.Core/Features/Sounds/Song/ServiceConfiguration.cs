using System;
using Frever.Cache.Configuration;
using Frever.Cache.Strategies;
using Frever.Client.Core.Features.Sounds.Song.DataAccess;
using Frever.Client.Shared.Files;
using Frever.ClientService.Contract.Sounds;
using Frever.Shared.MainDb.Entities;
using Microsoft.Extensions.DependencyInjection;
using Genre = Frever.Client.Core.Features.Sounds.Song.Models.Genre;

namespace Frever.Client.Core.Features.Sounds.Song;

public static class ServiceConfiguration
{
    public static void AddSongAsset(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<ISongAssetRepository, SongAssetRepository>();
        services.AddScoped<ISongAssetService, SongAssetService>();

        services.AddEntityFileConfiguration<SongFileConfig>();
        services.AddEntityFileConfiguration<PromotedSongDtoFileConfig>();

        services.AddFreverCaching(
            o =>
            {
                o.InMemory.Blob<Sounds.Song.Models.Song[]>(SerializeAs.Protobuf, false, typeof(Frever.Shared.MainDb.Entities.Song));
                o.InMemory.Blob<PromotedSongDto[]>(SerializeAs.Protobuf, false, typeof(PromotedSong));
                o.InMemory.Blob<Genre[]>(SerializeAs.Protobuf, false, typeof(Frever.Shared.MainDb.Entities.Genre));
            }
        );
    }
}