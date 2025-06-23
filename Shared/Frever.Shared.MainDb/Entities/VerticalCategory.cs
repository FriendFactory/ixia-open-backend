using System.Collections.Generic;
using Common.Models.Database.Interfaces;

namespace Frever.Shared.MainDb.Entities;

public class VerticalCategory : IEntity
{
    public long Id { get; set; }
    public long VerticalId { get; set; }
    public string Name { get; set; }

    public virtual Vertical Vertical { get; set; }
    public virtual ICollection<Video> Video { get; set; }
}