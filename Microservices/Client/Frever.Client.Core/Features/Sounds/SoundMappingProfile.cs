using System;
using AutoMapper;
using Frever.ClientService.Contract.Sounds;
using Frever.Shared.MainDb.Entities;
using Genre = Frever.Shared.MainDb.Entities.Genre;
using SongInfo = Frever.ClientService.Contract.Sounds.SongInfo;

namespace Frever.Client.Core.Features.Sounds;

// ReSharper disable once UnusedType.Global
public class SoundMappingProfile : Profile
{
    public SoundMappingProfile()
    {
        CreateMap<Frever.Shared.MainDb.Entities.Song, SongInfo>()
           .ForMember(d => d.Artist, o => o.MapFrom(s => s.Artist))
           .ForMember(d => d.Album, o => o.MapFrom(s => s.Album))
           .AfterMap((s, d, c) => { d.IsNew = s.CreatedTime > DateTime.UtcNow.AddDays(-c.GetNewAssetDays()); });

        CreateMap<Frever.Shared.MainDb.Entities.Song, Sounds.Song.Models.Song>()
           .ForMember(d => d.Artist, o => o.MapFrom(s => s.Artist))
           .ForMember(d => d.Album, o => o.MapFrom(s => s.Album));

        CreateMap<Sounds.Song.Models.Song, SongInfo>()
           .ForMember(d => d.Artist, o => o.MapFrom(s => s.Artist))
           .ForMember(d => d.Album, o => o.MapFrom(s => s.Album))
           .AfterMap((s, d, c) => { d.IsNew = s.CreatedTime > DateTime.UtcNow.AddDays(-c.GetNewAssetDays()); });

        CreateMap<PromotedSong, PromotedSongDto>();
        CreateMap<Genre, Sounds.Song.Models.Genre>().ReverseMap();
        CreateMap<Artist, ArtistInfo>();
        CreateMap<Album, AlbumInfo>();

        CreateMap<UserSound, UserSoundFullInfo>();
        CreateMap<UserSoundCreateModel, UserSound>();
    }
}

internal static class MappingExtensions
{
    public static void AddNewAssetDays(this IMappingOperationOptions options, int newAssetDays)
    {
        options.Items.Add(nameof(AddNewAssetDays), newAssetDays);
    }

    public static int GetNewAssetDays(this ResolutionContext context)
    {
        if (!context.TryGetItems(out var items))
            return 0;

        if (items.TryGetValue(nameof(AddNewAssetDays), out var value))
            return (int) value;

        return 0;
    }
}