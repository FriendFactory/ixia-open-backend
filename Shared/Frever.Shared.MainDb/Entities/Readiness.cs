using System.Collections.Generic;
using Common.Models.Database.Interfaces;

namespace Frever.Shared.MainDb.Entities;

public class Readiness : IEntity, IAdminCategory
{
    public const int KnownReadinessReady = 2;

    public Readiness()
    {
        Song = new HashSet<Song>();
        ExternalPlaylists = new HashSet<ExternalPlaylist>();
    }

    public long Id { get; set; }
    public string Name { get; set; }

    public virtual ICollection<Song> Song { get; set; }
    public virtual ICollection<ExternalPlaylist> ExternalPlaylists { get; set; }
}