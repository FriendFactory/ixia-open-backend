using System.Collections.Generic;
using System.Threading.Tasks;
using Frever.Shared.MainDb.Entities;
using Frever.Video.Contract;

namespace Frever.Video.Core.Features.Manipulation;

public interface IVideoManipulationService
{
    Task<VideoInfo> LikeVideo(long videoId);

    Task<VideoInfo> UnlikeVideo(long videoId);

    Task<VideoInfo> UpdateVideoAccess(long videoId, UpdateVideoAccessRequest model);

    Task<long[]> GetTaggedFriends(long videoId);

    Task<VideoInfo> SetPinned(long videoId, bool isPinned);

    Task<VideoInfo> UpdateVideo(long videoId, VideoPatchRequest request);

    Task DeleteVideo(long videoId);
}

public class VideoPatchRequest
{
    public bool IsLinksChanged { get; set; }
    public Dictionary<string, string> Links { get; set; }

    public bool? AllowComment { get; set; }

    public bool? AllowRemix { get; set; }
}

public class UpdateVideoAccessRequest
{
    public VideoAccess Access { get; set; }

    public long[] TaggedFriendIds { get; set; }

    public long? CrewId { get; set; }
}