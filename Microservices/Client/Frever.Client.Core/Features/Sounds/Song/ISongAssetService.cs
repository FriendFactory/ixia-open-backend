using System.Threading.Tasks;
using Frever.Client.Core.Features.Sounds.Song.Models;
using Frever.ClientService.Contract.Sounds;

namespace Frever.Client.Core.Features.Sounds.Song;

public interface ISongAssetService
{
    Task<SongInfo[]> GetSongListAsync(SongFilterModel model);

    Task<SongInfo> GetSongById(long id);

    Task<PromotedSongDto[]> GetPromotedSongs(int skip, int take);

    Task<Sounds.Song.Models.Song[]> GetSongs(long[] ids);

    Task<ExternalSongDto[]> GetAvailableExternalSongs(long[] ids);

    Task<ExternalSongDto> GetExternalSongById(long id);

    Task<Genre[]> GetAvailableGenres(string country = null);
}