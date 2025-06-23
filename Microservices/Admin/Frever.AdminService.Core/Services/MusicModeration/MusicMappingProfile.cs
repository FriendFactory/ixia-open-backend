using AutoMapper;
using Frever.AdminService.Core.Services.MusicModeration.Contracts;
using Frever.Shared.MainDb.Entities;
using Song = Frever.Shared.MainDb.Entities.Song;

namespace Frever.AdminService.Core.Services.MusicModeration;

// ReSharper disable once UnusedType.Global
public class MusicMappingProfile : Profile
{
    public MusicMappingProfile()
    {
        CreateMap<Song, SongDto>().ReverseMap();
        CreateMap<UserSound, UserSoundDto>().ReverseMap();
        CreateMap<PromotedSong, PromotedSongDto>().ReverseMap();

        CreateMap<Artist, ArtistDto>().ReverseMap();
        CreateMap<Album, AlbumDto>().ReverseMap();
        CreateMap<Genre, GenreDto>().ReverseMap();
        CreateMap<Label, LabelDto>().ReverseMap();
        CreateMap<Mood, MoodDto>().ReverseMap();
    }
}