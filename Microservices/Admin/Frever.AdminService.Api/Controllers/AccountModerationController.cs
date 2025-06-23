using System;
using System.Threading.Tasks;
using AuthServer.Permissions.Services;
using Frever.AdminService.Core.Services.AccountModeration;
using Microsoft.AspNetCore.Mvc;

#pragma warning disable CA2007

namespace Frever.AdminService.Api.Controllers;

[ApiController]
[Route("api/account/moderation")]
public class AccountModerationController(
    IAccountModerationService accountModerationService,
    IUserPermissionManagementService userPermissionManagementService
) : Controller
{
    private readonly IAccountModerationService _accountModerationService = accountModerationService ?? throw new ArgumentNullException(nameof(accountModerationService));
    private readonly IUserPermissionManagementService _userPermissionManagementService = userPermissionManagementService ?? throw new ArgumentNullException(nameof(userPermissionManagementService));

    [HttpPut]
    [Route("user-data")]
    public async Task<IActionResult> UpdateUserAuthData(UserAuthData model)
    {
        await _accountModerationService.UpdateUserAuthData(model);

        return NoContent();
    }

    [HttpPost]
    [Route("{groupId}/block")]
    public async Task<IActionResult> BlockGroup([FromRoute] long groupId)
    {
        await _userPermissionManagementService.SetGroupBlocked(groupId, true);

        return NoContent();
    }

    [HttpPost]
    [Route("{groupId}/unblock")]
    public async Task<IActionResult> UnblockGroup([FromRoute] long groupId)
    {
        await _userPermissionManagementService.SetGroupBlocked(groupId, false);

        return NoContent();
    }

    [HttpPut]
    [Route("{groupId}/undelete")]
    public async Task<IActionResult> UndeleteGroup([FromRoute] long groupId)
    {
        await _userPermissionManagementService.UndeleteGroup(groupId);

        return NoContent();
    }

    [HttpDelete]
    [Route("{groupId}/delete")]
    public async Task<IActionResult> DeleteGroup([FromRoute] long groupId)
    {
        await _accountModerationService.SoftDeleteGroup(groupId);

        return NoContent();
    }

    [HttpDelete]
    [Route("{groupId}/hard-delete")]
    public async Task<IActionResult> HardDeleteAccount(long groupId)
    {
        await _accountModerationService.HardDeleteGroup(groupId);

        return NoContent();
    }
}