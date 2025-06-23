using System;

namespace Frever.Shared.MainDb.Entities;

public class Reposts
{
    public long VideoId { get; set; }
    public long GroupId { get; set; }
    public DateTime Time { get; set; }

    public virtual Video Video { get; set; }
}