using System;
using System.Threading.Tasks;
using Frever.Video.Core.Features.ReportInappropriate;
using Microsoft.AspNetCore.Mvc;

#pragma warning disable CS8618

namespace Frever.Video.Api.Controllers;

[ApiController]
[Route("video")]
public class VideoReportController(IReportInappropriateVideoService reportVideoService) : ControllerBase
{
    private readonly IReportInappropriateVideoService _reportVideoService =
        reportVideoService ?? throw new ArgumentNullException(nameof(reportVideoService));

    [HttpPost]
    [Route("{videoId}/report")]
    public async Task<ActionResult> ReportVideo([FromRoute] long videoId, [FromBody] ReportVideoModel request)
    {
        var report = await _reportVideoService.ReportVideo(
                         new ReportInappropriateVideoRequest {Message = request.Message, ReasonId = request.ReasonId, VideoId = videoId}
                     );

        return Ok(new ReportVideoResponse {IncidentId = report.Id});
    }

    [HttpGet]
    [Route("report/moderation/reasons")]
    public async Task<ActionResult> GetReportingReason()
    {
        return Ok(await _reportVideoService.GetVideoReportReasons());
    }
}

public class ReportVideoModel
{
    public string Message { get; set; }

    public long ReasonId { get; set; }
}

public class ReportVideoResponse
{
    public long IncidentId { get; set; }
}