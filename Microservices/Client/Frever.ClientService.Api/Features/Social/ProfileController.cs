using System;
using System.Threading.Tasks;
using Frever.Client.Core.Features.Social.Profiles;
using Frever.ClientService.Contract.Social;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Frever.ClientService.Api.Features.Social;

[ApiController]
[Route("/api/group")]
public class ProfileController(IProfileService profileService, IPhoneLookupService phoneLookupService) : ControllerBase
{
    private readonly IPhoneLookupService _phoneLookupService = phoneLookupService ?? throw new ArgumentNullException(nameof(phoneLookupService));
    private readonly IProfileService _profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));

    /// <summary>
    ///     Who follow to target user
    /// </summary>
    [HttpGet("{groupId:long}")]
    public async Task<ActionResult<Profile>> GetProfileForGroup(long groupId)
    {
        var result = await _profileService.GetProfileAsync(groupId);
        if (result == null)
            return NotFound();

        return Ok(result);
    }

    [HttpGet("frever-official")]
    public async Task<IActionResult> GetFreverOfficialProfile()
    {
        var result = await _profileService.GetFreverOfficialProfile();
        if (result == null)
            return NotFound();

        return Ok(result);
    }

    [HttpGet("top")]
    public async Task<ActionResult<Profile>> GetTopProfiles(
        [FromQuery] string nickname,
        [FromQuery] int count = 20,
        [FromQuery] int skip = 0,
        [FromQuery] bool excludeMinors = false
    )
    {
        var result = await _profileService.GetTopProfiles(nickname, skip, count, excludeMinors);

        return Ok(result);
    }

    [HttpPost]
    [Route("lookup")]
    public async Task<IActionResult> LookupContacts([FromBody] string[] phoneNumbers)
    {
        if (phoneNumbers == null)
            return BadRequest();

        var result = await _phoneLookupService.LookupPhones(phoneNumbers);

        return Ok(result);
    }

    [HttpGet]
    [Route("start-to-follow-recommendations")]
    [ProducesResponseType(typeof(Profile[]), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStartToFollowRecommendations()
    {
        return Ok(await _profileService.GetStartFollowRecommendations());
    }

    [HttpPost]
    public async Task<IActionResult> GetGroupsShortInfo([FromBody] long[] groupIds)
    {
        var result = await _profileService.GetGroupsShortInfo(groupIds);

        return Ok(result);
    }
}