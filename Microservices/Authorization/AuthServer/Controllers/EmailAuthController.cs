using System;
using System.Threading.Tasks;
using AuthServer.Services.EmailAuth;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthServer.Controllers;

[AllowAnonymous]
[ApiController]
[Route("api")]
public class EmailAuthController(IEmailAuthService emailAuthService) : Controller
{
    private readonly IEmailAuthService _emailAuthService = emailAuthService ?? throw new ArgumentNullException(nameof(emailAuthService));

    [HttpPost]
    [Route("verify-email")]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request)
    {
        try
        {
            await _emailAuthService.SendEmailVerification(request);

            return NoContent();
        }
        catch (ValidationException ex)
        {
            return BadRequest(ex.Errors);
        }
    }
}