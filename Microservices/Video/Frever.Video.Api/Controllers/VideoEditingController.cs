using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Common.Infrastructure;
using Common.Infrastructure.Utils;
using Frever.Video.Contract;
using Frever.Video.Core.Features.Manipulation;
using IdentityModel.Client;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Frever.Video.Api.Controllers;

/// <summary>
///     Supports uploading and getting videos for level.
///     Uploading flow:
///     - POST /video/{levelId} - initializes new upload. Method checks if user is allowed to upload a video,
///     creates new upload and returns an URL to upload video to.
///     - (Externally) User uploads video file to given URL
///     - PUT /video/{levelId} -  commits upload. Checks if uploaded file matches passed size,
///     and inits video processing.
///     This flow solves following tasks:
///     - Decreases number of traffic goes through video server
///     - Prevents from uploading incomplete video (if user halts application in the middle of uploading)
/// </summary>
[ApiController]
[Authorize]
[Route("video")]
public class VideoEditingController(IVideoManipulationService videoSocialService) : ControllerBase
{
    private readonly IVideoManipulationService _videoSocialService =
        videoSocialService ?? throw new ArgumentNullException(nameof(videoSocialService));

    [HttpPost]
    [Route("{videoId}/like")]
    public async Task<ActionResult> LikeVideo([FromRoute] long videoId)
    {
        var info = await _videoSocialService.LikeVideo(videoId);

        return info == null ? NotFound() : Ok(info);
    }

    [HttpPost]
    [Route("{videoId}/unlike")]
    public async Task<ActionResult> UnlikeVideo([FromRoute] long videoId)
    {
        var info = await _videoSocialService.UnlikeVideo(videoId);

        return info == null ? NotFound() : Ok(info);
    }

    [HttpPost]
    [Route("{videoId}/access")]
    public async Task<ActionResult> UpdateVideoAccess([FromRoute] long videoId, [FromBody] UpdateVideoAccessRequest model)
    {
        var info = await _videoSocialService.UpdateVideoAccess(videoId, model);

        return info == null ? NotFound() : Ok(info);
    }

    [HttpPatch]
    [Route("{videoId}")]
    public async Task<IActionResult> PatchVideo([FromRoute] long videoId)
    {
        var body = await new StreamReader(Request.Body).ReadToEndAsync();

        var jRequest = JObject.Parse(body);

        var request = new VideoPatchRequest();
        var linksProp = nameof(VideoPatchRequest.Links).ToCamelCase();

        if (jRequest.ContainsKey(linksProp))
        {
            request.IsLinksChanged = true;
            var linksRaw = jRequest.TryGetString(linksProp);

            if (!string.IsNullOrWhiteSpace(linksRaw))
                request.Links = JsonConvert.DeserializeObject<Dictionary<string, string>>(linksRaw);
        }

        var allowCommentProp = nameof(VideoPatchRequest.AllowComment).ToCamelCase();
        if (jRequest.ContainsKey(allowCommentProp))
            request.AllowComment = jRequest.TryGetBoolean(allowCommentProp);

        var allowRemixProp = nameof(VideoPatchRequest.AllowRemix).ToCamelCase();
        if (jRequest.ContainsKey(allowRemixProp))
            request.AllowRemix = jRequest.TryGetBoolean(allowRemixProp);

        var updatedVideo = await _videoSocialService.UpdateVideo(videoId, request);

        return Ok(updatedVideo);
    }

    [HttpGet]
    [Route("{videoId}/tagged/friends")]
    public async Task<ActionResult> GetTaggedFriends([FromRoute] long videoId)
    {
        var result = await _videoSocialService.GetTaggedFriends(videoId);

        return Ok(result);
    }

    [HttpPut]
    [Route("{videoId}/pin")]
    [ProducesResponseType(typeof(VideoInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(AppErrorWithStatusCodeException), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Pin([FromRoute] long videoId)
    {
        var updated = await _videoSocialService.SetPinned(videoId, true);
        return Ok(updated);
    }

    [HttpDelete]
    [Route("{videoId}/pin")]
    [ProducesResponseType(typeof(VideoInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(AppErrorWithStatusCodeException), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UnPin([FromRoute] long videoId)
    {
        var updated = await _videoSocialService.SetPinned(videoId, false);
        return Ok(updated);
    }
}