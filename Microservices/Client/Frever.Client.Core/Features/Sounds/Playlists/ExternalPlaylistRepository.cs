using System.Linq;
using Frever.Shared.MainDb;
using Frever.Shared.MainDb.Entities;

namespace Frever.Client.Core.Features.Sounds.Playlists;

public interface IExternalPlaylistRepository
{
    IQueryable<ExternalPlaylist> GetPlaylists();
}

internal sealed class ExternalPlaylistRepository(IReadDb db) : IExternalPlaylistRepository
{
    public IQueryable<ExternalPlaylist> GetPlaylists()
    {
        return db.ExternalPlaylists;
    }
}