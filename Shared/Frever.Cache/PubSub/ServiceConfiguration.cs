using System;
using Microsoft.Extensions.DependencyInjection;

namespace Frever.Cache.PubSub;

public static class ServiceConfiguration
{
    /// <summary>
    ///     Call this if you need to publish messages.
    /// </summary>
    public static void AddRedisPublishing(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IPubSubPublisher, RedisPubSubPublisher>();
    }

    /// <summary>
    ///     Call this method if you need to subscribe and handle messages.
    /// </summary>
    public static void AddRedisSubscribing(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddHostedService<PubSubOrchestrator>();
    }

    public static void AddRedisSubscriber<TSubscriber>(this IServiceCollection services, ServiceLifetime lifetime = ServiceLifetime.Scoped)
        where TSubscriber : class, IPubSubSubscriber
    {
        ArgumentNullException.ThrowIfNull(services);

        services.Add(new ServiceDescriptor(typeof(IPubSubSubscriber), typeof(TSubscriber), lifetime));
    }

    public static void AddRedisSubscriber(
        this IServiceCollection services,
        Func<IServiceProvider, IPubSubSubscriber> factory,
        ServiceLifetime lifetime = ServiceLifetime.Scoped
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(factory);

        services.Add(new ServiceDescriptor(typeof(IPubSubSubscriber), factory, lifetime));
    }
}