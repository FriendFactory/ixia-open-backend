using System.Collections.Generic;

namespace Frever.Shared.MainDb.Entities;

public class Hashtag
{
    public Hashtag()
    {
        VideoAndHashtag = new HashSet<VideoAndHashtag>();
    }

    public long Id { get; set; }
    public string Name { get; set; }
    public long ViewsCount { get; set; }
    public long VideoCount { get; set; }
    public bool IsDeleted { get; set; }
    public long ChallengeSortOrder { get; set; }
    public virtual ICollection<VideoAndHashtag> VideoAndHashtag { get; set; }
}