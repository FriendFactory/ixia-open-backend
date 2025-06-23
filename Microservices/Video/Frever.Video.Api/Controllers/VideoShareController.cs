using System;
using System.Threading.Tasks;
using Frever.Video.Core.Features.Sharing;
using Frever.Videos.Shared.MusicGeoFiltering;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Frever.Video.Api.Controllers;

[ApiController]
[Authorize]
[Route("video")]
public class VideoShareController(
    IVideoShareService videoShareService,
    IPublicVideoContentService publicVideoContentService,
    ICurrentLocationProvider locationProvider
) : ControllerBase
{
    private readonly ICurrentLocationProvider _locationProvider =
        locationProvider ?? throw new ArgumentNullException(nameof(locationProvider));

    [HttpGet]
    [AllowAnonymous]
    [Route("watch/{videoShortGuid}")]
    public async Task<IActionResult> GetSharedVideo([FromRoute] string videoShortGuid, [FromQuery] string country)
    {
        if (string.IsNullOrWhiteSpace(country))
            country = (await _locationProvider.Get()).CountryIso3Code;

        var videoInfo = await videoShareService.GetSharedVideo(videoShortGuid, country);

        if (videoInfo == null)
            return NotFound();

        return Ok(videoInfo);
    }

    [HttpGet]
    [Route("{videoId}/sharing-info")]
    public async Task<ActionResult> GetVideoSharingInfo([FromRoute] long videoId)
    {
        var result = await publicVideoContentService.GetVideoSharingInfo(videoId);

        if (result == null)
            return NotFound();

        return Ok(result);
    }

    [HttpPost]
    [Route("share/{videoShortGuid}")]
    public async Task<ActionResult> AddVideoShare([FromRoute] string videoShortGuid)
    {
        await videoShareService.AddVideoShare(videoShortGuid);

        return NoContent();
    }
}