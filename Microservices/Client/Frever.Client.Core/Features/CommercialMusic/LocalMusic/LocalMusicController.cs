using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Frever.Client.Core.Features.CommercialMusic;

[ApiController]
[Route("api/music")]
[Authorize]
public class LocalMusicController(IMusicSearchService musicSearchService, I7DigitalProxyService proxy) : Controller
{
    private readonly I7DigitalProxyService _7digProxy = proxy ?? throw new ArgumentNullException(nameof(proxy));

    private readonly IMusicSearchService _musicSearchService =
        musicSearchService ?? throw new ArgumentNullException(nameof(musicSearchService));

    [HttpGet]
    [Route("search")]
    public async Task<IActionResult> Search(
        [FromQuery] string q,
        [FromQuery] string key,
        [FromQuery] int take = 20,
        [FromQuery] int skip = 0
    )
    {
        // key is not used in bridge right now (2024-04-18)
        var result = await _musicSearchService.Search(q, skip, take);
        return Ok(result);
    }

    [HttpGet]
    [Route("playlist/{id}")]
    public async Task<IActionResult> Playlist(string id)
    {
        var data = await _7digProxy.LoadPlaylistById(id);
        var content = data.ToString();
        return Content(content, "application/json");
    }
}