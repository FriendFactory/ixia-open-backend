using System.Threading.Tasks;
using AuthServerShared;
using Common.Infrastructure.Sounds;
using Frever.Video.Contract;
using Frever.Video.Core.Features.Feeds.Account;
using Frever.Video.Core.Features.Feeds.AiContent;
using Frever.Video.Core.Features.Feeds.Featured;
using Frever.Video.Core.Features.Feeds.Remixes;
using Frever.Video.Core.Features.Feeds.TaggedIn;
using Frever.Video.Core.Features.Feeds.Trending;
using Frever.Video.Core.Features.Feeds.UserSounds;
using Frever.Video.Core.Features.Hashtags.Feed;
using Frever.Video.Core.Features.PersonalFeed;
using Frever.Video.Core.Features.Shared;
using Frever.Video.Core.Features.Sharing;
using Frever.Video.Core.Features.Views;
using Frever.Videos.Shared.MusicGeoFiltering;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Frever.Video.Api.Controllers;

[ApiController]
[Authorize]
[Route("video")]
public class VideoViewController(
    IPersonalFeedService personalFeedService,
    IPersonalFeedRefreshingService personalFeedRefreshingService,
    ICurrentLocationProvider currentLocation,
    UserInfo currentUser,
    IHashtagVideoFeed hashtagVideoFeed,
    IUserSoundVideoFeed userSoundVideoFeed,
    ITrendingVideoFeed trendingVideoFeed,
    IFeaturedVideoFeed featuredVideoFeed,
    IRemixesOfVideoFeed remixesOfVideoFeed,
    ITaggedInVideoFeed taggedInVideoFeed,
    IAccountVideoFeed accountVideoFeed,
    IAiContentVideoFeed aiContentVideoFeed,
    IPublicVideoContentService publicVideoContentService,
    IOneVideoAccessor oneVideoAccessor,
    IVideoViewRecorder viewRecorder
) : ControllerBase
{
    /// <summary>
    ///     Geo-splitting: no
    ///     Music license: yes
    ///     Soft-split: no
    /// </summary>
    [HttpGet]
    [Route("sound/{soundId}/{type}")]
    public async Task<ActionResult> GetForSoundVideosFeed(
        [FromRoute] long soundId,
        [FromRoute] FavoriteSoundType type,
        [FromQuery(Name = "$targetVideo")] string targetVideo = null,
        [FromQuery(Name = "$takeNext")] int takeNext = 20
    )
    {
        var data = await userSoundVideoFeed.GetSoundVideoFeed(soundId, type, targetVideo, takeNext);

        return Ok(data);
    }

    /// <summary>
    ///     Geo-splitting: yes
    ///     Music license: yes
    ///     Soft-split: no
    /// </summary>
    [HttpGet]
    [Route("hashtag/{hashtagId}")]
    public async Task<ActionResult> GetForHashtagFeed(
        [FromRoute] long hashtagId,
        [FromQuery(Name = "$targetVideo")] string targetVideo = null,
        [FromQuery(Name = "$takeNext")] int takeNext = 20
    )
    {
        var data = await hashtagVideoFeed.GetHashtagVideoFeed(hashtagId, targetVideo, takeNext);

        return Ok(data);
    }

    /// <summary>
    ///     Geo-splitting: yes
    ///     Music license: yes
    ///     Soft-split: no
    /// </summary>
    [HttpGet]
    [Route("trending")]
    public async Task<ActionResult> GetTrendingVideos(
        [FromQuery(Name = "$targetVideo")] string targetVideo = null,
        [FromQuery(Name = "$takeNext")] int takeNext = 20
    )
    {
        var data = await trendingVideoFeed.GetTrendingVideos(targetVideo, takeNext);

        return Ok(data);
    }

    /// <summary>
    ///     Geo-splitting: no
    ///     Music license: yes
    ///     Soft-split: no
    /// </summary>
    [HttpGet]
    [Route("my-videos")]
    public async Task<IActionResult> GetMyVideos(
        [FromQuery(Name = "$targetVideo")] string targetVideo = null,
        [FromQuery(Name = "$takeNext")] int takeNext = 100,
        [FromQuery(Name = "$takePrevious")] int takePrevious = 0
    )
    {
        var data = await accountVideoFeed.GetMyVideos(targetVideo, takeNext, takePrevious);

        return Ok(data);
    }

    /// <summary>
    ///     Geo-splitting: no
    ///     Music license: yes
    ///     Soft-split: no
    /// </summary>
    [HttpGet]
    [Route("my-friends-videos")]
    public async Task<IActionResult> GetMyFriendsVideos(
        [FromQuery(Name = "$targetVideo")] string targetVideo = null,
        [FromQuery(Name = "$takeNext")] int takeNext = 100,
        [FromQuery(Name = "$takePrevious")] int takePrevious = 0
    )
    {
        var data = await accountVideoFeed.GetMyFriendsVideos(targetVideo, takeNext, takePrevious);

        return Ok(data);
    }

    /// <summary>
    ///     Geo-splitting: no
    ///     Music license: yes
    ///     Soft-split: no
    /// </summary>
    [HttpGet]
    [Route("my-following")]
    public async Task<IActionResult> GetMyFollowingFeedVideo(
        [FromQuery(Name = "$targetVideo")] string targetVideo = null,
        [FromQuery(Name = "$takeNext")] int takeNext = 100,
        [FromQuery(Name = "$takePrevious")] int takePrevious = 0
    )
    {
        var data = await accountVideoFeed.MyFollowingFeedVideo(targetVideo, takeNext, takePrevious);

        return Ok(data);
    }

    /// <summary>
    ///     Geo-splitting: yes
    ///     Music license: yes
    ///     Soft-split: no
    /// </summary>
    [HttpGet]
    [Route("fyp")]
    public async Task<IActionResult> GetVideoForYourPageV2(
        [FromQuery(Name = "$targetVideo")] string targetVideo = null,
        [FromQuery(Name = "$takeNext")] int takeNext = 100,
        [FromQuery(Name = "refresh")] string refresh = null
    )
    {
        if (Request.Query.ContainsKey("refresh"))
        {
            var location = await currentLocation.Get();
            await personalFeedRefreshingService.RefreshFeed(currentUser, location.Lon, location.Lat);
        }

        var (data, version) = await personalFeedService.PersonalFeed(currentUser, targetVideo, takeNext);
        Response.Headers.Append("X-FYP-FEED-VERSION", version.ToString());

        return Ok(data);
    }

    /// <summary>
    ///     Geo-splitting: no
    ///     Music license: yes
    ///     Soft-split: no
    /// </summary>
    [HttpGet]
    [Route("by-group/{groupId}")]
    public async Task<IActionResult> GetGroupVideos(
        [FromRoute] long groupId,
        [FromQuery(Name = "$targetVideo")] string targetVideo = null,
        [FromQuery(Name = "$takeNext")] int takeNext = 100,
        [FromQuery(Name = "$takePrevious")] int takePrevious = 0
    )
    {
        var data = await accountVideoFeed.GetGroupVideos(groupId, targetVideo, takeNext, takePrevious);

        if (data == null)
            return NotFound();

        return Ok(data);
    }

    /// <summary>
    ///     Geo-splitting: no
    ///     Music license: yes
    ///     Soft-split: no
    /// </summary>
    [HttpGet]
    [Route("tagged/{groupId}")]
    public async Task<IActionResult> GetVideosUserTaggedIn(
        [FromRoute] long groupId,
        [FromQuery(Name = "$targetVideo")] string targetVideo = null,
        [FromQuery(Name = "$takeNext")] int takeNext = 100,
        [FromQuery(Name = "$takePrevious")] int takePrevious = 0
    )
    {
        var data = await taggedInVideoFeed.VideoUserTaggedIn(groupId, targetVideo, takeNext, takePrevious);

        return Ok(data);
    }

    /// <summary>
    ///     Geo-splitting: no
    ///     Music license: yes
    ///     Soft-split: no
    /// </summary>
    [HttpGet]
    [Route("{videoId}/remixes")]
    public async Task<IActionResult> GetRemixesOfVideo(
        [FromRoute] long videoId,
        [FromQuery(Name = "$targetVideo")] string targetVideo = null,
        [FromQuery(Name = "$takeNext")] int takeNext = 100,
        [FromQuery(Name = "$takePrevious")] int takePrevious = 0
    )
    {
        var data = await remixesOfVideoFeed.GetRemixesOfVideo(videoId, targetVideo, takeNext, takePrevious);

        return Ok(data);
    }

    /// <summary>
    ///     Geo-splitting: yes
    ///     Music license: yes
    ///     Soft-split: no
    /// </summary>
    [HttpGet]
    [Route("featured")]
    public async Task<ActionResult> GetFeaturedVideos(
        [FromQuery(Name = "$targetVideo")] string targetVideo = null,
        [FromQuery(Name = "$takeNext")] int takeNext = 100,
        [FromQuery(Name = "$takePrevious")] int takePrevious = 0
    )
    {
        var data = await featuredVideoFeed.FeaturedVideos(targetVideo, takeNext, takePrevious);

        return Ok(data);
    }

    [HttpGet]
    [Route("ai-content/{id}")]
    public async Task<IActionResult> GetAiContentVideo([FromRoute] long id)
    {
        var data = await aiContentVideoFeed.GetAiContentVideo(id);
        return Ok(data);
    }

    /// <summary>
    ///     Geo-splitting: no
    ///     Music license: yes
    ///     Soft-split: no
    /// </summary>
    [HttpGet]
    [Route("{videoId}")]
    public async Task<ActionResult> GetVideoInfo([FromRoute] long videoId)
    {
        var videoInfo = await oneVideoAccessor.GetVideo(FetchVideoInfoFrom.WriteDb, currentUser, videoId);

        if (videoInfo == null)
            return NotFound();

        return Ok(videoInfo);
    }

    /// <summary>
    ///     Geo-splitting: no
    ///     Music license: yes
    ///     Soft-split: no
    /// </summary>
    [HttpGet]
    [Route("{videoId}/view")]
    public async Task<ActionResult> VideoContent([FromRoute] long videoId)
    {
        var video = await publicVideoContentService.GetMyOrPublicVideoContent(videoId);

        if (video == null)
            return NotFound();

        return Ok(video);
    }

    /// <summary>
    ///     Geo-splitting: no
    ///     Music license: yes
    ///     Soft-split: no
    /// </summary>
    [HttpGet]
    [Route("{videoId}/player-url")]
    public async Task<ActionResult> GetVideoPlayerUrl([FromRoute] long videoId)
    {
        var result = await publicVideoContentService.GetVideoPlayerUrl(videoId);

        if (result == null)
            return NotFound();

        return Ok(result);
    }

    /// <summary>
    ///     Geo-splitting: no
    ///     Music license: yes
    ///     Soft-split: no
    /// </summary>
    [HttpGet]
    [Route("{videoId}/file-url")]
    public async Task<ActionResult> GetVideoSingleFileUrl([FromRoute] long videoId)
    {
        var result = await publicVideoContentService.GetVideoSingleFileUrl(videoId);

        if (result == null)
            return NotFound();

        return Ok(result);
    }

    [HttpPost]
    [Route("views")]
    public async Task<ActionResult> RecordViewViews([FromBody] ViewViewInfo[] views)
    {
        await viewRecorder.RecordVideoView(views);

        return NoContent();
    }
}