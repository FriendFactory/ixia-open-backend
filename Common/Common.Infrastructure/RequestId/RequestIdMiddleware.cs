using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using OpenTelemetry.Trace;

namespace Common.Infrastructure.RequestId;

// ReSharper disable once ClassNeverInstantiated.Global
public class RequestIdMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext httpContext)
    {
        var traceId = Tracer.CurrentSpan.Context.TraceId.ToString();

        var requestId = string.IsNullOrWhiteSpace(traceId)
                            ? httpContext.Request.Headers[HttpContextHeaderAccessor.XRequestIdHeader].FirstOrDefault() ??
                              Guid.NewGuid().ToString()
                            : traceId;

        httpContext.Response.Headers.Append(HttpContextHeaderAccessor.XRequestIdHeader, requestId);

        await next(httpContext);
    }
}