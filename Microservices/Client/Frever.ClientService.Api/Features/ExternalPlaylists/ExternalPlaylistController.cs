using System;
using System.Threading.Tasks;
using Frever.Client.Core.Features.Sounds.Playlists;
using Frever.ClientService.Contract.Sounds;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Frever.ClientService.Api.Features.ExternalPlaylists;

[Authorize]
[Route("api/external-playlists")]
public class ExternalPlaylistController : ControllerBase
{
    private readonly IExternalPlaylistService _externalPlaylistService;

    public ExternalPlaylistController(IExternalPlaylistService externalPlaylistService)
    {
        _externalPlaylistService = externalPlaylistService ?? throw new ArgumentNullException(nameof(externalPlaylistService));
    }

    [HttpGet]
    [Route("{id}")]
    [ProducesResponseType(typeof(ExternalPlaylistInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetExternalPlaylistById([FromRoute] long id)
    {
        var externalPlaylist = await _externalPlaylistService.GetById(id);

        if (externalPlaylist == null)
            return NotFound();

        return Ok(externalPlaylist);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ExternalPlaylistInfo[]), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetExternalPlaylists([FromBody] ExternalPlaylistFilterModel model)
    {
        var result = await _externalPlaylistService.GetPlaylists(model);

        return Ok(result);
    }
}