namespace Frever.Shared.MainDb.Entities;

public class VideoMention
{
    public long VideoId { get; set; }
    public long GroupId { get; set; }

    public virtual Video Video { get; set; }

    public virtual Group Group { get; set; }
}