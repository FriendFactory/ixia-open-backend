using AutoMapper;
using Frever.ClientService.Contract.Sounds;

namespace Frever.Client.Core.Features.Sounds.Playlists;

internal class ExternalPlaylistMappingProfile : Profile
{
    public ExternalPlaylistMappingProfile()
    {
        CreateMap<Frever.Shared.MainDb.Entities.ExternalPlaylist, ExternalPlaylistModel>();
        CreateMap<ExternalPlaylistModel, ExternalPlaylistInfo>();
    }
}