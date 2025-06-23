using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using IPNetwork = Microsoft.AspNetCore.HttpOverrides.IPNetwork;

namespace Common.Infrastructure.InternalRequest;

public static class InternalRequestExtension
{
    public static bool IsInternalRequest(this HttpContext httpContext)
    {
        var logFactory = httpContext.RequestServices.GetRequiredService<ILoggerFactory>();
        var log = logFactory.CreateLogger("Frever.InternalRequest");

        using var logScope = log.BeginScope(
            "Requesting internal endpoint at {Host}/{Path}: ",
            httpContext.Request.Host,
            httpContext.Request.Path
        );

        var localIp = httpContext.Connection.LocalIpAddress;
        var remoteIp = httpContext.Connection.RemoteIpAddress;

        var metricsRequest = httpContext.Request.Path.StartsWithSegments("/metrics");

        if (httpContext.Request.Headers.TryGetValue("X-Forwarded-For", out var headerValue))
        {
            log.LogWarning("Request was forwarded via load balancer from {Value}, access denied", headerValue);
            return false;
        }

        if (!metricsRequest)
            log.LogInformation("From {RemoteIP} to {LocalIP} ", remoteIp, localIp);

        if (IPAddress.IsLoopback(remoteIp))
        {
            log.LogInformation("Allow request from loopback (localhost) ({Ip})", IPAddress.Loopback);
            return true;
        }

        var network = new IPNetwork(localIp, 16);
        if (!network.Contains(remoteIp))
        {
            log.LogError("Remote address {Remote} doesn't belong to network {Nw}", remoteIp, network.Prefix);
            return false;
        }

        if (!metricsRequest)
            log.LogInformation("Remote address {Remote} belongs to the same network {Nw}", remoteIp, network.Prefix);

        return true;
    }
}