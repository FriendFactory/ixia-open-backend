using System.Threading.Tasks;
using Common.Infrastructure;
using Frever.Video.Core.Features.Manipulation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Frever.Video.Api.Controllers;

[ApiController]
[Authorize]
[Route("video")]
public class VideoDeleteController(IVideoManipulationService deletionService) : ControllerBase
{
    [HttpDelete]
    [Route("{videoId}")]
    public async Task<ActionResult> DeleteVideo([FromRoute] long videoId)
    {
        try
        {
            await deletionService.DeleteVideo(videoId);

            return Ok();
        }
        catch (AppErrorWithStatusCodeException ex)
        {
            return StatusCode((int) ex.StatusCode, ex.Message);
        }
    }
}