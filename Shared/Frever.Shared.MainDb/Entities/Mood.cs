using System.Collections.Generic;
using Common.Models.Database.Interfaces;

namespace Frever.Shared.MainDb.Entities;

public class Mood : IEntity
{
    public long Id { get; set; }
    public string Name { get; set; }

    public virtual ICollection<Song> Song { get; set; }
}