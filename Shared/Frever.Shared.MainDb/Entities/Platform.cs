using System.Collections.Generic;

namespace Frever.Shared.MainDb.Entities;

public class Platform
{
    public Platform()
    {
        Video = new HashSet<Video>();
    }

    public int Id { get; set; }
    public string Name { get; set; }

    public virtual ICollection<Video> Video { get; set; }
}