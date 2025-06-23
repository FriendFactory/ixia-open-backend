using System.Linq;
using Frever.Client.Core.Utils;
using Frever.Shared.MainDb;
using Frever.Shared.MainDb.Entities;
using Microsoft.EntityFrameworkCore;

namespace Frever.Client.Core.Features.Sounds.Song.DataAccess;

internal sealed class SongAssetRepository(IReadDb db) : ISongAssetRepository
{
    public IQueryable<Frever.Shared.MainDb.Entities.Song> GetSongs()
    {
        return db.Song.OrderBy(e => e.SortOrder).AccessibleForEveryone().AsNoTracking();
    }

    public IQueryable<PromotedSong> GetPromotedSongs()
    {
        return db.PromotedSong.Where(e => e.IsEnabled).OrderBy(e => e.SortOrder).AsNoTracking();
    }

    public IQueryable<Genre> GetGenres()
    {
        return db.Genre;
    }

    public IQueryable<ExternalSong> GetAvailableExternalSongIds(params long[] externalSongIds)
    {
        return GetAvailableExternalSongs().Where(e => externalSongIds.Contains(e.Id));
    }

    public IQueryable<ExternalSong> GetAvailableExternalSongs()
    {
        return db.ExternalSongs.Where(e => !e.IsManuallyDeleted && !e.IsDeleted && e.NotClearedSince == null);
    }
}