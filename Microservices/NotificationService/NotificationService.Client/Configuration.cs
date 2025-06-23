using System;
using Common.Infrastructure.ServiceDiscovery;
using Frever.Cache.PubSub;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace NotificationService.Client;

public static class Configuration
{
    public static void AddNotificationServiceClient(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddRedisPublishing();
        services.AddServiceUrls(configuration);
        services.AddHttpClient();
        services.AddScoped<INotificationAddingService, NotificationPubSubAddServiceClient>();
    }
}