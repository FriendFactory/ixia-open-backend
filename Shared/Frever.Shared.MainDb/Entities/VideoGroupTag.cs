namespace Frever.Shared.MainDb.Entities;

public class VideoGroupTag
{
    public long VideoId { get; set; }
    public long GroupId { get; set; }
    public bool IsCharacterTag { get; set; }

    public virtual Video Video { get; set; }

    public virtual Group Group { get; set; }
}