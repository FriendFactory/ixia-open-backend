using System.Threading.Tasks;
using Frever.ClientService.Contract.Sounds;

namespace Frever.Client.Core.Features.Sounds.Playlists;

public interface IExternalPlaylistService
{
    Task<ExternalPlaylistInfo[]> GetPlaylists(ExternalPlaylistFilterModel model);
    Task<ExternalPlaylistInfo> GetById(long id);
}