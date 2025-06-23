using System;
using System.Threading.Tasks;
using AuthServer.Contracts;
using AuthServer.Features.CredentialUpdate;
using AuthServer.Features.CredentialUpdate.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthServer.Controllers;

[ApiController]
[Authorize(AuthenticationSchemes = "Bearer")]
[Route("api/credential")]
public class CredentialUpdateController(ICredentialUpdateService service) : Controller
{
    private readonly ICredentialUpdateService _service = service ?? throw new ArgumentNullException(nameof(service));

    [HttpGet]
    [Route("status")]
    public async Task<IActionResult> GetCredentialStatus()
    {
        var result = await _service.GetCredentialStatus();

        return Ok(result);
    }

    [HttpPost]
    [Route("verify")]
    public async Task<IActionResult> VerifyCredentials([FromBody] VerifyCredentialRequest request)
    {
        await _service.VerifyCredentials(request);

        return NoContent();
    }

    [HttpPost]
    [Route("verify/user")]
    public async Task<IActionResult> VerifyUser([FromBody] VerifyUserRequest request)
    {
        var result = await _service.VerifyUser(request);
        if (!result.IsSuccessful)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> AddCredentials([FromBody] AddCredentialsRequest request)
    {
        await _service.AddCredentials(request);

        return NoContent();
    }

    [HttpPut]
    public async Task<IActionResult> UpdateCredentials([FromBody] UpdateCredentialsRequest request)
    {
        await _service.UpdateCredentials(request);

        return NoContent();
    }

    [HttpPost]
    [Route("username")]
    public async Task<IActionResult> UpdateUserName([FromBody] UpdateUserNameRequest request)
    {
        var result = await _service.UpdateUserName(request);
        if (!result.Ok)
            return BadRequest(result);

        return Ok(result);
    }
}