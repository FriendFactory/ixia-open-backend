using System.Net;
using System.Threading.Tasks;
using Common.Infrastructure.InternalRequest;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Frever.AdminService.Api.Utils;

public class UrlAccessMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/metrics"))
            if (!context.IsInternalRequest())
            {
                context.Response.StatusCode = (int) HttpStatusCode.Unauthorized;
                return;
            }

        await next.Invoke(context).ConfigureAwait(false);
    }
}