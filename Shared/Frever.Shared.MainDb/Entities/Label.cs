using System.Collections.Generic;
using Common.Models.Database.Interfaces;

namespace Frever.Shared.MainDb.Entities;

public class Label : IEntity
{
    public Label()
    {
        Song = new HashSet<Song>();
    }

    public long Id { get; set; }
    public string Name { get; set; }

    public virtual ICollection<Song> Song { get; set; }
    public virtual ICollection<Genre> Genre { get; set; }
}