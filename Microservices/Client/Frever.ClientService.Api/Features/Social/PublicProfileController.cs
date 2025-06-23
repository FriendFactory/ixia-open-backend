using System;
using System.Threading.Tasks;
using Frever.Client.Core.Features.Social.PublicProfiles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Frever.ClientService.Api.Features.Social;

[ApiController]
[Route("/api/group/public")]
public class PublicProfileController(IPublicProfileService publicProfileService) : ControllerBase
{
    private readonly IPublicProfileService _publicProfileService = publicProfileService ?? throw new ArgumentNullException(nameof(publicProfileService));

    [AllowAnonymous]
    [HttpGet("{nickname}")]
    public async Task<IActionResult> GetPublicProfile(string nickname)
    {
        var result = await _publicProfileService.GetPublicProfile(nickname);
        if (result == null)
            return NotFound();

        return Ok(result);
    }
}