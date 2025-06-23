using System.Threading.Tasks;
using Frever.Video.Core.Features.CreatePage;
using Microsoft.AspNetCore.Mvc;

namespace Frever.Video.Api.Controllers;

[ApiController]
[Route("create-page")]
public class CreatePageController(ICreatePageService service) : ControllerBase
{
    [HttpGet]
    [Route("content")]
    public async Task<IActionResult> GetCreatePageContent([FromQuery] string testGroup = null)
    {
        var result = await service.GetCreatePageContent(testGroup);

        return Ok(result);
    }

    [HttpGet]
    [Route("row/{id}/hashtags")]
    public async Task<ActionResult> GetRowHashtags([FromRoute] long id, [FromQuery] string target = null, [FromQuery] int takeNext = 10)
    {
        var result = await service.GetRowHashtags(id, target, takeNext);

        return Ok(result);
    }

    [HttpGet]
    [Route("row/{id}/songs")]
    public async Task<ActionResult> GetRowSongs([FromRoute] long id, [FromQuery] string target = null, [FromQuery] int takeNext = 10)
    {
        var result = await service.GetRowSongs(id, target, takeNext);

        return Ok(result);
    }

    [HttpGet]
    [Route("row/{id}/videos")]
    public async Task<ActionResult> GetRowVideos([FromRoute] long id, [FromQuery] string target = null, [FromQuery] int takeNext = 10)
    {
        var result = await service.GetRowVideos(id, target, takeNext);

        return Ok(result);
    }

    [HttpGet]
    [Route("row/{id}/images")]
    public async Task<ActionResult> GetRowImages([FromRoute] long id, [FromQuery] string target = null, [FromQuery] int takeNext = 10)
    {
        var result = await service.GetRowImages(id, target, takeNext);

        return Ok(result);
    }
}