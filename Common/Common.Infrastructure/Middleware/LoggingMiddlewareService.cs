using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Common.Infrastructure.Middleware;

public class LoggingMiddlewareService(ILogger<LoggingMiddlewareService> logger, RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext httpContext)
    {
        var values = new List<string>();

        var requestId = httpContext.Response.Headers["X-Request-Id"].FirstOrDefault() ??
                        httpContext.Request.Headers["X-Request-Id"].FirstOrDefault();
        if (!string.IsNullOrEmpty(requestId))
            values.Add($"RequestId(Jaeger-Trace-Id)={requestId}");

        var groupId = GetRequesterGroupId(httpContext);
        if (groupId != null)
            values.Add($"RequestedByGroupId={groupId}");

        var sessionId = httpContext.Request.Headers["X-Session-Id"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(sessionId))
            values.Add($"SessionId={sessionId}");

        values.Add($"HttpMethod={httpContext.Request.Method}");

        using (logger.BeginScope($"{string.Join(", ", values)} |>>>"))
        {
            await next(httpContext);
        }
    }

    private static string GetRequesterGroupId(HttpContext context)
    {
        var token = context.Request.Headers.Authorization.FirstOrDefault()?.Split(" ").Last();
        if (string.IsNullOrEmpty(token))
            return null;

        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);

            var groupId = jwtToken.Claims.First(x => x.Type.Equals("PrimaryGroupId"));

            return groupId?.Value;
        }
        catch
        {
            return null;
        }
    }
}