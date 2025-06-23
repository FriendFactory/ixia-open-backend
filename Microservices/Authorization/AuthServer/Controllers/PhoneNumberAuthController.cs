using System;
using System.Threading.Tasks;
using AuthServer.Services.PhoneNumberAuth;
using Common.Infrastructure;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthServer.Controllers;

[AllowAnonymous]
[ApiController]
[Route("api")]
public class PhoneNumberAuthController(IPhoneNumberAuthService phoneNumberAuthService) : Controller
{
    private readonly IPhoneNumberAuthService _phoneNumberAuthService =
        phoneNumberAuthService ?? throw new ArgumentNullException(nameof(phoneNumberAuthService));

    [HttpPost]
    [Route("verify-phone-number")]
    public async Task<IActionResult> VerifyPhoneNumber([FromBody] VerifyPhoneNumberRequest request)
    {
        try
        {
            var result = await _phoneNumberAuthService.SendPhoneNumberVerification(request);

            if (result.IsSuccessful)
                return Ok(result);

            return BadRequest(result);
        }
        catch (ValidationException ex)
        {
            return BadRequest(ex.Errors);
        }
        catch (AppErrorWithStatusCodeException ex)
        {
            return StatusCode((int) ex.StatusCode, ex.Message);
        }
    }
}