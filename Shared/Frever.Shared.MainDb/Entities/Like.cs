using System;

namespace Frever.Shared.MainDb.Entities;

public class Like
{
    public long VideoId { get; set; }

    public long UserId { get; set; }

    public DateTime Time { get; set; }

    public virtual Video Video { get; set; }
}