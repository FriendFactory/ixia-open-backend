using System.Collections.Generic;
using Common.Models.Database.Interfaces;

namespace Frever.Shared.MainDb.Entities;

public class Genre : IEntity, IAdminCategory
{
    public Genre()
    {
        BrandAndGenre = new HashSet<BrandAndGenre>();
        Song = new HashSet<Song>();
    }

    public long Id { get; set; }
    public string Name { get; set; }
    public int SortOrder { get; set; }
    public long LabelId { get; set; }
    public string[] Countries { get; set; }

    public virtual Label Label { get; set; }
    public virtual ICollection<BrandAndGenre> BrandAndGenre { get; set; }
    public virtual ICollection<Song> Song { get; set; }
}