using System;

namespace Frever.Shared.MainDb.Entities;

public class VideoView
{
    public long VideoId { get; set; }

    public long UserId { get; set; }

    public DateTime Time { get; set; }

    public string FeedType { get; set; }

    public string FeedTab { get; set; }
}