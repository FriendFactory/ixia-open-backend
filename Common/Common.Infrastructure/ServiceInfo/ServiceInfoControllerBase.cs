using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Common.Infrastructure.ServiceInfo;

[ApiController]
[Route("service")]
public class ServiceInfoControllerBase<T> : ControllerBase
    where T : class
{
    [AllowAnonymous]
    [HttpGet("info")]
    public virtual ActionResult GetInfo()
    {
        var assembly = typeof(T).Assembly.GetName();
        var data = new Common.Infrastructure.ServiceInfo.ServiceInfo {Version = assembly.Version.ToString(), Name = assembly.Name};

        return Ok(data);
    }
}