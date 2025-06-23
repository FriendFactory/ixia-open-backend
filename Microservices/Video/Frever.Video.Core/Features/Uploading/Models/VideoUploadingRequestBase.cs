using System.Collections.Generic;
using Common.Infrastructure.RegexUtils;
using Common.Models;
using Frever.Shared.MainDb.Entities;
using Newtonsoft.Json;

namespace Frever.Video.Core.Features.Uploading.Models;

public class VideoUploadingRequestBase
{
    public string Description { get; set; }

    public VideoAccess Access { get; set; }

    public int DurationSec { get; set; }

    public int Size { get; set; }

    public long PublishTypeId { get; set; }

    public VideoOrientation VideoOrientation { get; set; } = VideoOrientation.Portrait;

    public long[] TaggedFriendIds { get; set; } = [];

    public Dictionary<string, string> Links { get; set; }

    [JsonIgnore] public IReadOnlyList<string> Hashtags => RegexHelper.GetMatches(Description, RegexPatterns.Hashtags);

    [JsonIgnore] public IReadOnlyList<string> Mentions => RegexHelper.GetMatches(Description, RegexPatterns.Mentions);

    public bool AllowRemix { get; set; } = true;

    public bool AllowComment { get; set; } = true;
}