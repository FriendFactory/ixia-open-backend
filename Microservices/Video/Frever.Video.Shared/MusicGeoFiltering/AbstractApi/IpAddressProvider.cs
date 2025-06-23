using System;
using System.Net;
using Microsoft.AspNetCore.Http;

namespace Frever.Videos.Shared.MusicGeoFiltering.AbstractApi;

public interface IIpAddressProvider
{
    IPAddress GetIpAddressOfConnectedClient();
}

public class HttpContextIpAddressProvider(IHttpContextAccessor httpContextAccessor) : IIpAddressProvider
{
    public IPAddress GetIpAddressOfConnectedClient()
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext == null)
            throw new InvalidOperationException("HttpContext is not available");

        var theirIp = httpContext.Connection.RemoteIpAddress;
        if (httpContext.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedIp))
            theirIp = IPAddress.Parse(forwardedIp);

        return theirIp;
    }
}