using System;
using System.Threading.Tasks;
using Common.Infrastructure.Caching;
using Microsoft.AspNetCore.Mvc;

#pragma warning disable CA2007

namespace Frever.AdminService.Api.Controllers;

[Route("api/cache")]
[ApiController]
public class CacheController(ICache cache) : ControllerBase
{
    private readonly ICache _cache = cache ?? throw new ArgumentNullException(nameof(cache));

    [HttpPost]
    [Route("reset")]
    public async Task<IActionResult> ResetCache()
    {
        await _cache.ClearCache();

        return NoContent();
    }
}