using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Common.Infrastructure.Utils;
using Common.Models.Files;
using FluentValidation;
using Frever.Client.Core.Features.Social.MyProfileInfo;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Frever.ClientService.Api.Features.Social;

[ApiController]
[Route("/api/me")]
public class MyProfileController(IMyProfileService myProfileService) : ControllerBase
{
    private readonly IMyProfileService _myProfileService = myProfileService ?? throw new ArgumentNullException(nameof(myProfileService));

    [HttpPost]
    [Route("status/online")]
    public async Task<IActionResult> SetOnlineStatus()
    {
        await _myProfileService.SetMyStatusOnline();

        return NoContent();
    }

    [HttpGet]
    public async Task<IActionResult> Me()
    {
        var me = await _myProfileService.Me();

        if (me == null)
            return NotFound();

        return Ok(me);
    }

    [HttpGet]
    [Route("balance")]
    public async Task<IActionResult> GetMyBalance()
    {
        var userBalance = await _myProfileService.GetMyBalance();

        return Ok(userBalance);
    }

    [HttpPost]
    [Route("balance/initial")]
    public async Task<IActionResult> AddInitialBalance()
    {
        await _myProfileService.AddInitialBalance();

        return NoContent();
    }

    [HttpPut]
    public async Task<IActionResult> UpdateMe([FromBody] UpdateProfileRequest request)
    {
        try
        {
            var me = await _myProfileService.UpdateProfile(request);

            if (me == null)
                return NotFound();

            return Ok(me);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new {Ok = false, Error = ex.Message, ex.Errors});
        }
    }

    [HttpPatch]
    public async Task<IActionResult> PatchMe([FromBody] Dictionary<string, object> request)
    {
        try
        {
            var current = await _myProfileService.Me();
            if (current == null)
                return NotFound();

            var hasChanges = false;
            var update = new UpdateProfileRequest {Bio = current.Bio, BioLinks = current.BioLinks};

            if (request.TryGetValue(nameof(update.Bio).ToCamelCase(), out var bioObj))
            {
                hasChanges = true;
                update.Bio = bioObj.ToString();
            }

            if (request.TryGetValue(nameof(update.BioLinks).ToCamelCase(), out var linksObj))
            {
                hasChanges = true;
                update.BioLinks = JsonConvert.DeserializeObject<Dictionary<string, string>>(linksObj.ToString());
            }

            if (request.TryGetValue(nameof(update.Files).ToCamelCase(), out var filesObj))
            {
                hasChanges = true;
                update.Files = JsonConvert.DeserializeObject<FileMetadata[]>(filesObj.ToString());
            }

            return hasChanges ? Ok(await _myProfileService.UpdateProfile(update)) : Ok(current);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new {Ok = false, Error = ex.Message, ex.Errors});
        }
    }

    [HttpPost]
    [Route("advertising/tracking/{appsFlyerId}")]
    public async Task<IActionResult> AddMyMyAdvertisingTracking(string appsFlyerId)
    {
        await _myProfileService.AddMyMyAdvertisingTracking(appsFlyerId);

        return NoContent();
    }

    [HttpDelete]
    [Route("advertising/tracking")]
    public async Task<IActionResult> DeleteMyAdvertisingTracking()
    {
        await _myProfileService.DeleteMyAdvertisingTracking();

        return NoContent();
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteMe()
    {
        await _myProfileService.DeleteMe();

        return NoContent();
    }
}