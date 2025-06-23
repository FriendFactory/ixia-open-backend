using System.Threading.Tasks;
using AssetServer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

#pragma warning disable CS1998

namespace AssetServer.Controllers;

[ApiController]
[Route("api")]
public class ConfigsController(IConfigsService configsService) : Controller
{
    [AllowAnonymous]
    [HttpGet]
    [Route("Configs")]
    public async Task<IActionResult> GetFilesConfigs()
    {
        var output = configsService.GetConfigsInfo();
        return Ok(output);
    }
}