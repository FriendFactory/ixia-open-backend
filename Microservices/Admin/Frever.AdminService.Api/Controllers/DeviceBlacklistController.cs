using System.Threading.Tasks;
using Frever.AdminService.Core.Services.DeviceBlacklist;
using Microsoft.AspNetCore.Mvc;

namespace Frever.AdminService.Api.Controllers;

[Route("/api/device-blacklist")]
public class DeviceBlacklistController(IDeviceBlacklistAdminService deviceBlacklistAdminService) : ControllerBase
{
    [HttpGet]
    [Route("")]
    public async Task<IActionResult> GetDeviceBlacklist([FromQuery] string search, [FromQuery] int skip = 0, [FromQuery] int take = 30)
    {
        return Ok(await deviceBlacklistAdminService.GetDeviceBlacklist(search, skip, take));
    }

    [HttpPost]
    [Route("")]
    public async Task<IActionResult> BlockDevice([FromBody] BlockDeviceParams request)
    {
        var blocked = await deviceBlacklistAdminService.BlockDevice(request);
        return Ok(blocked);
    }

    [HttpDelete]
    [Route("")]
    public async Task<IActionResult> UnblockDevice([FromBody] BlockDeviceParams request)
    {
        await deviceBlacklistAdminService.UnblockDevice(request.DeviceId);
        return NoContent();
    }
}