using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Frever.Videos.Shared.MusicGeoFiltering;
using Microsoft.AspNetCore.Mvc;

namespace Frever.AdminService.Api.Controllers;

[ApiController]
[Route("api/my-ip")]
public class MyIpController(ICurrentLocationProvider currentLocation) : ControllerBase
{
    private readonly ICurrentLocationProvider _currentLocation =
        currentLocation ?? throw new ArgumentNullException(nameof(currentLocation));

    [HttpGet]
    [Route("")]
    public async Task<IActionResult> GetMyIp()
    {
        var theirIp = HttpContext.Connection.RemoteIpAddress;
        if (Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedIp))
            theirIp = IPAddress.Parse(forwardedIp);

        var country = string.Empty;

        try
        {
            country = (await _currentLocation.Get()).CountryIso3Code;
        }
        catch (Exception ex)
        {
            country = ex.ToString();
        }

        return Ok(
            new
            {
                Ip = theirIp.ToString(),
                Country = country,
                Headers = HttpContext.Request.Headers.Select(h => $"{h.Key}: {h.Value}").ToArray()
            }
        );
    }
}