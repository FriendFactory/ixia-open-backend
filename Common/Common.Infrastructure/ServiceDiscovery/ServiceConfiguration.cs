using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Common.Infrastructure.ServiceDiscovery;

public static class ServiceConfiguration
{
    public static ServiceUrls AddServiceUrls(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var urls = configuration.GetSection("ServiceDiscovery").Get<ServiceUrls>();
        urls.Validate();

        services.AddSingleton(urls);

        return urls;
    }
}