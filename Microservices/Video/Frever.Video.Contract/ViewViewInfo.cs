using System;

namespace Frever.Video.Contract;

public class ViewViewInfo
{
    public long VideoId { get; set; }
    public DateTime ViewDate { get; set; }
    public string FeedType { get; set; }
    public string FeedTab { get; set; }
}