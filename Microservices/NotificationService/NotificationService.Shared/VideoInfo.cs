namespace NotificationService.Shared;

public class VideoInfo
{
    public long Id { get; set; }
}

public class VideoDetails : VideoInfo
{
    public long? RemixedFromVideoId { get; set; }

    public long GroupId { get; set; }

    public int Access { get; set; }

    public long[] CharacterTags { get; set; }

    public long[] NonCharacterTags { get; set; }

    public long[] Mentions { get; set; }
}