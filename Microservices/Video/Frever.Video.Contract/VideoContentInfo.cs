using System.Collections.Generic;

#pragma warning disable CS8618

namespace Frever.Video.Contract;

public class VideoContentInfo
{
    public long Id { get; set; }
    public long GroupId { get; set; }
    public string RedirectUrl { get; set; }
    public string SharingUrl { get; set; }
    public string PlayerUrl { get; set; }
    public string ThumbnailUrl { get; set; }
    public string SingleFileVideoUrl { get; set; }
    public Dictionary<string, string> SignedCookies { get; set; }
}