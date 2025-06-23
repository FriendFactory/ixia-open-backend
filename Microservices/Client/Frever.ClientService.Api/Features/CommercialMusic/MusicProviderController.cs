using System;
using System.Threading.Tasks;
using Frever.Client.Core.Features.CommercialMusic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Frever.ClientService.Api.Features.CommercialMusic;

[ApiController]
[Route("api/MusicProvider")]
[Authorize]
public class MusicProviderController(IMusicProviderService musicProviderService) : Controller
{
    private readonly IMusicProviderService _musicProviderService = musicProviderService ?? throw new ArgumentNullException(nameof(musicProviderService));

    [HttpPost]
    [Route("SignUrl")]
    public async Task<IActionResult> GetSignedUrl([FromBody] SignUrlRequest request)
    {
        var result = await _musicProviderService.GetSignedRequestData(request);

        return Ok(result);
    }
}