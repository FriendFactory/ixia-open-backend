using System;
using AuthServer.Services;
using AuthServer.Services.BridgeSDK;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace AuthServer.Controllers;

[AllowAnonymous]
[ApiController]
[Route("api/Client")]
public class ClientVersionController(IClientVersionService clientVersionService, IConfiguration configuration) : Controller
{
    private readonly IClientVersionService _clientVersionService = clientVersionService ?? throw new ArgumentNullException(nameof(clientVersionService));
    private readonly IConfiguration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

    [HttpGet]
    [Route("SupportedVersions")]
    public ActionResult<ClientVersions> BridgeSupportedVersions()
    {
        var version = _clientVersionService.GetSupportedVersions();

        return Ok(version);
    }

    [HttpGet]
    [Route("Urls")]
    public IActionResult Urls()
    {
        var urls = _configuration.GetExternalUrlConfiguration();
        return Ok(urls);
    }
}