using AuthServer.Models;
using Microsoft.Extensions.Configuration;

namespace AuthServer.Services;

public static class ServiceUrlInfoProvider
{
    public static ExternalUrls GetExternalUrlConfiguration(this IConfiguration config)
    {
        var result = config.GetSection("ExternalUrls").Get<ExternalUrls>();
        result.Validate();
        return result;
    }
}