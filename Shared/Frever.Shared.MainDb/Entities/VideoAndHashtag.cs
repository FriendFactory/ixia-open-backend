namespace Frever.Shared.MainDb.Entities;

public class VideoAndHashtag
{
    public long VideoId { get; set; }
    public long HashtagId { get; set; }

    public virtual Hashtag Hashtag { get; set; }
    public virtual Video Video { get; set; }
}