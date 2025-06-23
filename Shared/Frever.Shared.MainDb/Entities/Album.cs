using System.Collections.Generic;
using Common.Models.Database.Interfaces;

namespace Frever.Shared.MainDb.Entities;

public class Album : IEntity
{
    public Album()
    {
        Song = new HashSet<Song>();
    }

    public long Id { get; set; }
    public string Name { get; set; }
    public long? ArtistId { get; set; }

    public virtual Artist Artist { get; set; }
    public virtual ICollection<Song> Song { get; set; }
}