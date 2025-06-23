using System;
using System.Threading.Tasks;
using Frever.AdminService.Core.Services.Social;
using Frever.AdminService.Core.Services.Social.Contracts;
using Frever.Shared.MainDb.Entities;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNetCore.Mvc;

#pragma warning disable CA2007

namespace Frever.AdminService.Api.Controllers;

[ApiController]
[Route("api/profile")]
public class ProfileController(IProfileService profileService) : Controller
{
    private readonly IProfileService _profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));

    [HttpGet]
    public async Task<IActionResult> GetProfiles(ODataQueryOptions<ProfileDto> options)
    {
        var result = await _profileService.GetProfiles(options);

        return Ok(result);
    }

    [HttpGet("by/{propertyName}")]
    public async Task<IActionResult> GetProfilesOrderedBy(
        [FromRoute] string propertyName,
        [FromQuery] bool? isFeatured,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] int take = 20,
        [FromQuery] int skip = 0
    )
    {
        var result = await _profileService.GetProfilesOrderedBy(
                         propertyName,
                         isFeatured,
                         startDate,
                         endDate,
                         take,
                         skip
                     );

        return Ok(result);
    }

    [HttpGet("{groupId}/kpi")]
    public async Task<IActionResult> GetProfileKpi([FromRoute] long groupId)
    {
        var result = await _profileService.GetProfileKpiByGroupId(groupId);

        return Ok(result);
    }

    [HttpGet]
    [Route("{groupId}/activity")]
    public async Task<IActionResult> GetUserActivity(
        [FromRoute] long groupId,
        [FromQuery] UserActionType? actionType,
        ODataQueryOptions<UserActivityDto> options
    )
    {
        var result = await _profileService.GetUserActivity(options, groupId, actionType);

        return Ok(result);
    }
}