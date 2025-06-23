using System.Collections.Generic;
using Common.Models.Database.Interfaces;

namespace Frever.Shared.MainDb.Entities;

public class Brand : IEntity, IGroupAccessible, IAdminCategory
{
    public Brand()
    {
        BrandAndGenre = new HashSet<BrandAndGenre>();
    }

    public long Id { get; set; }
    public long GroupId { get; set; }
    public string Name { get; set; }

    public virtual Group Group { get; set; }
    public virtual ICollection<BrandAndGenre> BrandAndGenre { get; set; }
}