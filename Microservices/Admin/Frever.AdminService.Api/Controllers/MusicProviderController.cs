using System.Threading.Tasks;
using Frever.AdminService.Core.Services.MusicProvider;
using Microsoft.AspNetCore.Mvc;

#pragma warning disable CA2007

namespace Frever.AdminService.Api.Controllers;

[ApiController]
[Route("api/music-provider")]
public class MusicProviderController(IMusicProviderService musicProviderService) : ControllerBase
{
    [HttpPost]
    [Route("request")]
    public async Task<IActionResult> SendMusicProviderRequest([FromBody] MusicProviderRequest request)
    {
        var result = await musicProviderService.SendMusicProviderRequest(request);

        return Ok(result);
    }

    [HttpPost]
    [Route("sign-url")]
    public async Task<IActionResult> SignMusicProviderUrl([FromBody] MusicProviderRequest request)
    {
        var result = await musicProviderService.SignMusicProviderUrl(request);

        return Ok(result);
    }
}