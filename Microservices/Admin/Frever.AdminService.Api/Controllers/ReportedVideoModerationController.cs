using System;
using System.Threading.Tasks;
using Common.Infrastructure;
using Frever.AdminService.Core.Services.VideoModeration;
using Frever.AdminService.Core.Services.VideoModeration.Contracts;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNetCore.Mvc;

#pragma warning disable CA2007

namespace Frever.AdminService.Api.Controllers;

[ApiController]
[Route("api/video-report/moderation")]
public class ReportedVideoModerationController(IVideoModerationService videoModerationService) : ControllerBase
{
    private readonly IVideoModerationService _videoModerationService = videoModerationService ?? throw new ArgumentNullException(nameof(videoModerationService));

    [HttpGet]
    public async Task<ActionResult> GetReportedVideos(ODataQueryOptions<VideoReportDto> options)
    {
        var result = await _videoModerationService.GetVideoReportInfo(options);

        return Ok(result);
    }

    [HttpGet]
    [Route("reasons")]
    public async Task<ActionResult> GetReportingReason()
    {
        var result = await _videoModerationService.GetVideoReportReasons();

        return Ok(result);
    }

    [HttpPost]
    [Route("{id}/hide-video")]
    public async Task<ActionResult> HideVideo([FromRoute] long id)
    {
        try
        {
            var result = await _videoModerationService.SetVideoHidden(id, true);

            return Ok(result);
        }
        catch (AppErrorWithStatusCodeException ex)
        {
            return StatusCode((int) ex.StatusCode, ex.Message);
        }
    }

    [HttpPost]
    [Route("{id}/unhide-video")]
    public async Task<ActionResult> UnhideVideo([FromRoute] long id)
    {
        try
        {
            var result = await _videoModerationService.SetVideoHidden(id, false);

            return Ok(result);
        }
        catch (AppErrorWithStatusCodeException ex)
        {
            return StatusCode((int) ex.StatusCode, ex.Message);
        }
    }

    [HttpPost]
    [Route("{id}/close-incident")]
    public async Task<ActionResult> CloseIncident([FromRoute] long id)
    {
        try
        {
            var result = await _videoModerationService.CloseIncident(id);

            return Ok(result);
        }
        catch (AppErrorWithStatusCodeException ex)
        {
            return StatusCode((int) ex.StatusCode, ex.Message);
        }
    }

    [HttpPost]
    [Route("{id}/reopen-incident")]
    public async Task<ActionResult> ReopenIncident([FromRoute] long id)
    {
        try
        {
            var result = await _videoModerationService.ReopenIncident(id);

            return Ok(result);
        }
        catch (AppErrorWithStatusCodeException ex)
        {
            return StatusCode((int) ex.StatusCode, ex.Message);
        }
    }
}