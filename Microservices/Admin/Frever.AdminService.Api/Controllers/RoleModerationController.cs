using System;
using System.Threading.Tasks;
using Frever.AdminService.Core.Services.RoleModeration;
using Microsoft.AspNetCore.Mvc;

#pragma warning disable CA2007

namespace Frever.AdminService.Api.Controllers;

[Route("api/role/moderation")]
[ApiController]
public class RoleModerationController(IRoleModerationService service) : ControllerBase
{
    private readonly IRoleModerationService _service = service ?? throw new ArgumentNullException(nameof(service));

    [HttpGet]
    [Route("access-scope")]
    public async Task<IActionResult> GetAccessScopes()
    {
        var result = await _service.GetAccessScopes();

        return Ok(result);
    }

    [HttpGet]
    [Route("access-scope/{groupId}")]
    public async Task<IActionResult> GetUserAccessScopes([FromRoute] long groupId)
    {
        var result = await _service.GetUserAccessScopes(groupId);

        return Ok(result);
    }

    [HttpGet]
    [Route("role")]
    public async Task<IActionResult> GetRoles([FromQuery] int skip = 0, [FromQuery] int take = 20)
    {
        var result = await _service.GetRoles(skip, take);

        return Ok(result);
    }

    [HttpGet]
    [Route("user")]
    public async Task<IActionResult> GetUserRoles(
        [FromQuery] string email,
        [FromQuery] long? roleId,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20
    )
    {
        var result = await _service.GetUserRoles(email, roleId, skip, take);

        return Ok(result);
    }

    [HttpPost]
    [Route("role")]
    public async Task<IActionResult> SaveRole(RoleModel model)
    {
        await _service.SaveRole(model);

        return NoContent();
    }

    [HttpPost]
    [Route("user")]
    public async Task<IActionResult> SaveUserRole(UserRoleModel model)
    {
        await _service.SaveUserRole(model);

        return NoContent();
    }

    [HttpDelete]
    [Route("role/{id}")]
    public async Task<IActionResult> DeleteRole(long id)
    {
        await _service.DeleteRole(id);

        return NoContent();
    }
}