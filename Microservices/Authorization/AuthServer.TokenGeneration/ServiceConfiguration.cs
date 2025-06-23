using System;
using Frever.Cache.PubSub;
using Microsoft.Extensions.DependencyInjection;

namespace AuthServer.TokenGeneration;

public static class ServiceConfiguration
{
    public static void AddTokenGeneration(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<ITokenGenerationClient, RpcTokenGenerationClient>();
        services.AddRedisPublishing();
        services.AddRedisSubscribing();

        services.AddRedisSubscriber(
            svc => svc.GetRequiredService<ITokenGenerationClient>() as RpcTokenGenerationClient,
            ServiceLifetime.Singleton
        );
    }
}