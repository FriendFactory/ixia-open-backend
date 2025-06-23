using System.Linq;
using Frever.Shared.MainDb.Entities;

namespace Frever.Client.Core.Features.Sounds.Song.DataAccess;

public interface ISongAssetRepository
{
    IQueryable<Frever.Shared.MainDb.Entities.Song> GetSongs();

    IQueryable<PromotedSong> GetPromotedSongs();

    IQueryable<Genre> GetGenres();

    IQueryable<ExternalSong> GetAvailableExternalSongIds(params long[] externalSongIds);
}