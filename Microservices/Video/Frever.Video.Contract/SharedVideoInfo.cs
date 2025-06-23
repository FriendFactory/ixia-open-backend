using System;
using System.Collections.Generic;
using Frever.ClientService.Contract.Social;
using Frever.Shared.MainDb.Entities;

#pragma warning disable CS8618

namespace Frever.Video.Contract;

public class SharedVideoInfo
{
    public long VideoId { get; set; }
    public DateTime CreatedTime { get; set; }
    public string VideoFileUrl { get; set; }
    public long[] ExternalSongIds { get; set; }
    public SongInfo[] Songs { get; set; }
    public GroupShortInfo Owner { get; set; }
    public VideoKpi Kpi { get; set; }
    public long FollowersCount { get; set; }
    public GroupShortInfo CurrentGroup { get; set; }
    public string Description { get; set; }
    public string[] Hashtags { get; set; }
    public Dictionary<long, string> SongLabels { get; set; }
}