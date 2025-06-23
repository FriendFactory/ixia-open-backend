using System.Threading.Tasks;
using Common.Infrastructure.TargetInfoMiddleware;
using Microsoft.AspNetCore.Http;

namespace Common.Infrastructure.Middleware;

public class TargetInfoMiddleware(TargetInfo targetInfo, RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext httpContext)
    {
        if (!string.IsNullOrWhiteSpace(targetInfo?.TargetIdentifier))
            httpContext.Response.Headers.Append("X-Target-Id", targetInfo.TargetIdentifier);

        await next(httpContext);
    }
}