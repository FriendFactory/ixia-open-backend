using System.Collections.Generic;
using Common.Models.Database.Interfaces;

namespace Frever.Shared.MainDb.Entities;

public class Gender : IEntity
{
    public Gender()
    {
        Group = new HashSet<Group>();
    }

    public long Id { get; set; }
    public long RaceId { get; set; }
    public string Name { get; set; }
    public string Identifier { get; set; }
    public string UmaRaceKey { get; set; }
    public string UpperUnderwearOverlay { get; set; }
    public string LowerUnderwearOverlay { get; set; }
    public bool CanCreateCharacter { get; set; }
    public bool IsEnabled { get; set; }
    public virtual ICollection<Group> Group { get; set; }
}