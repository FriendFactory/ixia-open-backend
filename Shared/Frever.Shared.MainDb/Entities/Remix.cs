using System;

namespace Frever.Shared.MainDb.Entities;

public class Remix
{
    public long VideoId { get; set; }

    public long RemixerGroupId { get; set; }

    public DateTime Time { get; set; }
}