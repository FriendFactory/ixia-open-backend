using System;
using Amazon.SimpleNotificationService;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Common.Infrastructure.Messaging;

public static class SnsMessagingConfiguration
{
    public static void AddSnsMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddAWSService<IAmazonSimpleNotificationService>();
        services.AddOptions<SnsMessagingSettings>().Bind(configuration.GetSection(nameof(SnsMessagingSettings)));
        services.AddScoped<ISnsMessagingService, SnsMessagingService>();
    }
}