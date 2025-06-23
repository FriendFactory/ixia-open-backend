using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Common.Infrastructure;
using Frever.Client.Core.Features.Social.Followers;
using Frever.ClientService.Contract.Social;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Frever.ClientService.Api.Features.Social;

[ApiController]
[Route("/api/group/")]
public class FollowingController(IFollowingService followingService) : ControllerBase
{
    private readonly IFollowingService _followingService = followingService ?? throw new ArgumentNullException(nameof(followingService));

    [HttpGet]
    [Route("follow-recommendations")]
    [ProducesResponseType(typeof(FollowRecommendation[]), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AppErrorWithStatusCodeException), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetFollowRecommendations()
    {
        var result = await _followingService.GetPersonalizedFollowRecommendations();
        return Ok(result);
    }

    [HttpGet]
    [Route("follow-back-recommendations")]
    [ProducesResponseType(typeof(FollowRecommendation[]), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AppErrorWithStatusCodeException), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetFollowBackRecommendations()
    {
        var result = await _followingService.GetFollowBackRecommendations();
        return Ok(result);
    }

    /// <summary>
    ///     Get who an user follow
    /// </summary>
    [HttpGet("{groupId}/following")]
    public async Task<ActionResult<IEnumerable<Profile>>> GetGroupsFollowedByAnUser(
        [FromRoute] long groupId,
        [FromQuery] string nickname,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20
    )
    {
        var result = await _followingService.GetFollowedProfilesAsync(groupId, nickname, skip, take);

        return Ok(result);
    }

    [HttpGet("{groupId}/follower")]
    public async Task<ActionResult<IEnumerable<Profile>>> GetFollowers(
        [FromRoute] long groupId,
        [FromQuery] string nickname,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20
    )
    {
        var result = await _followingService.GetFollowersProfilesAsync(groupId, nickname, skip, take);

        return Ok(result);
    }

    [HttpGet("{groupId}/friends")]
    public async Task<IActionResult> GetFriends(
        [FromRoute] long groupId,
        [FromQuery] string nickname,
        [FromQuery] bool startChatOnly = false,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20
    )
    {
        var result = await _followingService.GetFriendProfilesAsync(
                         groupId,
                         nickname,
                         startChatOnly,
                         skip,
                         take
                     );

        return Ok(result);
    }

    [HttpPost("{groupId}/follower")]
    public async Task<ActionResult> StartFollow(long groupId)
    {
        var result = await _followingService.FollowGroupAsync(groupId);
        if (result == null)
            return NotFound();

        return Ok(result);
    }

    [HttpDelete("{groupId}/follower")]
    public async Task<ActionResult> StopFollow(long groupId)
    {
        await _followingService.UnFollowGroupAsync(groupId);

        return Ok();
    }
}