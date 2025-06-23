using System.Collections.Generic;
using Common.Models.Database.Interfaces;

namespace Frever.Shared.MainDb.Entities;

public class Artist : IEntity, IAdminCategory
{
    public Artist()
    {
        Album = new HashSet<Album>();
        Song = new HashSet<Song>();
    }

    public long Id { get; set; }
    public string Name { get; set; }

    public virtual ICollection<Album> Album { get; set; }
    public virtual ICollection<Song> Song { get; set; }
}