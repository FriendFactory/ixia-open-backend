using System;
using System.Threading.Tasks;
using Frever.AdminService.Core.Services.UserActionSetting;
using Frever.Client.Shared.ActivityRecording;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

#pragma warning disable CA2007

namespace Frever.AdminService.Api.Controllers;

[ApiController]
[Route("api/user-action-setting")]
public class UserActionSettingController(IUserActionSettingService userActionSettingService) : ControllerBase
{
    private readonly IUserActionSettingService _userActionSettingService = userActionSettingService ?? throw new ArgumentNullException(nameof(userActionSettingService));

    [HttpGet]
    [Route("")]
    [ProducesResponseType(typeof(UserActivitySettings), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSettings()
    {
        return Ok(await _userActionSettingService.GetSettingsAsync());
    }

    [HttpPost]
    [Route("")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateOrUpdate([FromBody] UserActivitySettings settings)
    {
        var result = await _userActionSettingService.CreateOrUpdateAsync(settings);

        return Ok(result);
    }
}