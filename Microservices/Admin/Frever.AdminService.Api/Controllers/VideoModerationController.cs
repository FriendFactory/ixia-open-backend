using System;
using System.Threading.Tasks;
using Common.Infrastructure;
using Frever.AdminService.Core.Services.VideoModeration;
using Frever.AdminService.Core.Services.VideoModeration.Contracts;
using Frever.AdminService.Core.Utils;
using Frever.Shared.MainDb.Entities;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

#pragma warning disable CA2007

namespace Frever.AdminService.Api.Controllers;

[ApiController]
[Route("api/video/moderation")]
public class VideoModerationController(IVideoModerationService videoModerationService) : ControllerBase
{
    private static readonly JsonSerializerSettings JsonSerializerSettings =
        new() {ContractResolver = new CamelCasePropertyNamesContractResolver()};

    private readonly IVideoModerationService _videoModerationService =
        videoModerationService ?? throw new ArgumentNullException(nameof(videoModerationService));

    [HttpGet]
    public async Task<ActionResult> GetVideoFeed(
        bool? isFeatured,
        VideoAccess? access,
        string countryIso3,
        string languageIso3,
        ODataQueryOptions<VideoDto> options
    )
    {
        try
        {
            var query = await _videoModerationService.GetAllVideos(isFeatured, access, countryIso3, languageIso3);

            var result = await query.ExecuteODataRequestWithCount(options);
            result.Data = await _videoModerationService.WithAiContent(result.Data);

            return Ok(result);
        }
        catch (AppErrorWithStatusCodeException ex)
        {
            return StatusCode((int) ex.StatusCode, ex.Message);
        }
    }

    [HttpGet]
    [Route("{id}")]
    public async Task<ActionResult> GetVideo([FromRoute] long id)
    {
        try
        {
            var video = await _videoModerationService.GetVideoById(id);

            if (video == null)
                return NotFound();

            await _videoModerationService.WithAiContent([video]);

            var contentInfo = await _videoModerationService.ToVideoContentInfo(video);

            return Ok(new {video, media = contentInfo, remixes = await _videoModerationService.GetVideosRemixedFromVideo(id)});
        }
        catch (AppErrorWithStatusCodeException ex)
        {
            return StatusCode((int) ex.StatusCode, ex.Message);
        }
    }

    [HttpGet]
    [Route("comments")]
    public async Task<IActionResult> GetComments(ODataQueryOptions<ModerationCommentInfo> options)
    {
        var data = await _videoModerationService.GetComments(options);

        return Ok(data);
    }

    [HttpDelete]
    [Route("{videoId}/comments/{commentId}")]
    public async Task<IActionResult> DeleteModerationVideoComment([FromRoute] long videoId, [FromRoute] long commentId)
    {
        await _videoModerationService.SetCommentDeleted(videoId, commentId);

        return NoContent();
    }

    [HttpPut]
    [Route("{id}/soft-delete")]
    public async Task<ActionResult> SetSoftDelete([FromRoute] long id, [FromBody] SetSoftDeleteParameters param)
    {
        ArgumentNullException.ThrowIfNull(param);

        try
        {
            var video = await _videoModerationService.SetSoftDelete(id, param.IsDeleted, param.IncludeRemixes);
            if (video == null)
                return NotFound();

            var contentInfo = await _videoModerationService.ToVideoContentInfo(video);

            return Ok(new {video, media = contentInfo, remixes = await _videoModerationService.GetVideosRemixedFromVideo(id)});
        }
        catch (AppErrorWithStatusCodeException ex)
        {
            return StatusCode((int) ex.StatusCode, ex.Message);
        }
    }

    [HttpDelete]
    [Route("hard-delete-account-data/{groupId}")]
    public async Task<IActionResult> HardDeleteAccountData([FromRoute] long groupId)
    {
        await _videoModerationService.HardDeleteAccountData(groupId);

        return NoContent();
    }

    [HttpPost]
    [Route("publish/{videoId}")]
    public async Task<ActionResult> PublishVideo([FromRoute] long videoId)
    {
        await _videoModerationService.PublishVideo(videoId);

        return NoContent();
    }

    [HttpPost]
    [Route("unpublish/{videoId}")]
    public async Task<ActionResult> UnPublishVideo([FromRoute] long videoId, [FromQuery] bool includeRemixes)
    {
        await _videoModerationService.UnPublishVideo(videoId, includeRemixes);

        return NoContent();
    }

    [HttpPatch]
    [Route("{videoId}")]
    public async Task<IActionResult> PatchVideo([FromRoute] long videoId, [FromBody] JObject jRequest)
    {
        var request = new VideoPatchRequest();

        JsonConvert.PopulateObject(jRequest.ToString(), request, JsonSerializerSettings);

        await _videoModerationService.UpdateVideo(videoId, request);

        return NoContent();
    }

    [HttpPost]
    [Route("soft-delete-by-hashtag-id")]
    public async Task<ActionResult> SoftDeleteByHashtagId([FromBody] VideoModerationByHashtagParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        try
        {
            await _videoModerationService.SoftDeleteVideosByHashtagId(parameters.HashtagId, parameters.IncludeRemixes);

            return NoContent();
        }
        catch (AppErrorWithStatusCodeException ex)
        {
            return StatusCode((int) ex.StatusCode, ex.Message);
        }
    }

    [HttpPost]
    [Route("unpublish-by-hashtag-id")]
    public async Task<ActionResult> UnPublishByHashtagId([FromBody] VideoModerationByHashtagParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        try
        {
            await _videoModerationService.UnPublishVideosByHashtagId(parameters.HashtagId, parameters.IncludeRemixes);

            return NoContent();
        }
        catch (AppErrorWithStatusCodeException ex)
        {
            return StatusCode((int) ex.StatusCode, ex.Message);
        }
    }
}

public class SetSoftDeleteParameters
{
    public bool IsDeleted { get; set; }

    public bool IncludeRemixes { get; set; }
}

public class VideoModerationByHashtagParameters
{
    public long HashtagId { get; set; }
    public bool IncludeRemixes { get; set; }
}