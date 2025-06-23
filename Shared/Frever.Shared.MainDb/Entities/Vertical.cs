using System.Collections.Generic;
using Common.Models.Database.Interfaces;

namespace Frever.Shared.MainDb.Entities;

public class Vertical : IEntity
{
    public Vertical()
    {
        VerticalCategory = new HashSet<VerticalCategory>();
    }

    public long Id { get; set; }
    public string Name { get; set; }

    public virtual ICollection<VerticalCategory> VerticalCategory { get; set; }
}