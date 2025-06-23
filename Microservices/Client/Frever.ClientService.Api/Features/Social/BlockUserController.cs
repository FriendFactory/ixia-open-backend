using System;
using System.Threading.Tasks;
using FluentValidation;
using Frever.Client.Core.Features.Social.GroupBlocking;
using Microsoft.AspNetCore.Mvc;

namespace Frever.ClientService.Api.Features.Social;

[ApiController]
[Route("/api/group")]
public class BlockUserController(IBlockUserService blockUserService) : ControllerBase
{
    private readonly IBlockUserService _blockUserService = blockUserService ?? throw new ArgumentNullException(nameof(blockUserService));

    [HttpGet]
    [Route("blocked-users")]
    public async Task<IActionResult> GetBlockedProfiles()
    {
        var result = await _blockUserService.GetBlockedProfiles();

        return Ok(result);
    }

    [HttpPost]
    [Route("block/{groupId}")]
    public async Task<IActionResult> BlockUser(long groupId)
    {
        try
        {
            await _blockUserService.BlockUser(groupId);

            return Ok();
        }
        catch (ValidationException ex)
        {
            return BadRequest(new {Ok = false, Error = ex.Message, ex.Errors});
        }
    }

    [HttpPost]
    [Route("unblock/{groupId}")]
    public async Task<IActionResult> UnBlockUser(long groupId)
    {
        await _blockUserService.UnBlockUser(groupId);

        return Ok();
    }
}