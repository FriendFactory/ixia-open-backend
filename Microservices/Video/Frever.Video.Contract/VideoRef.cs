using Frever.Shared.MainDb.Entities;

namespace Frever.Video.Contract;

public class VideoRef
{
    public long Id { get; set; }

    public long GroupId { get; set; }

    public long SortOrder { get; set; }

    public SongInfo[] SongInfo { get; set; }

    public override string ToString()
    {
        return $"Id={Id} GroupId={GroupId} SortOrder={SortOrder}";
    }
}