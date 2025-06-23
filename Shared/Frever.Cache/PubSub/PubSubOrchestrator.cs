using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Frever.Cache.PubSub;

internal class PubSubOrchestrator : IHostedService
{
    private readonly ILogger _log;
    private readonly IConnectionMultiplexer _redis;
    private readonly IServiceProvider _serviceProvider;

    private ISubscriber _subscriber;

    public PubSubOrchestrator(IConnectionMultiplexer redis, ILoggerFactory loggerFactory, IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);

        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _log = loggerFactory.CreateLogger("Frever.Cache.PubSub");
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _log.LogInformation("Orchestrator started");

        _subscriber = _redis.GetSubscriber();

        using var scope = _serviceProvider.CreateScope();

        var subscribers = scope.ServiceProvider.GetServices<IPubSubSubscriber>();

        foreach (var sub in subscribers)
        {
            _subscriber.Subscribe(sub.SubscriptionKey, OnMessage);
            _log.LogInformation("Subscribed to {key}", sub.SubscriptionKey);
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _subscriber?.UnsubscribeAll();

        _log.LogInformation("Orchestrator stopped");

        return Task.CompletedTask;
    }

    private void OnMessage(RedisChannel key, RedisValue message)
    {
        string subscriptionKey = key;

        _log.LogInformation("{key} message received", key);

        using var scope = _serviceProvider.CreateScope();

        var subscribers = scope.ServiceProvider.GetServices<IPubSubSubscriber>();

        var sub = subscribers.FirstOrDefault(a => StringComparer.OrdinalIgnoreCase.Equals(a.SubscriptionKey, subscriptionKey));
        if (sub == null)
        {
            _log.LogWarning("No subscribers for message {key}", key);
            return;
        }

        using var logScope = _log.BeginScope("{key} handler {type}:: ", key, sub.GetType().Name);

        try
        {
            _log.LogTrace("Calling handler");

            sub.OnMessage(message).Wait();

            _log.LogTrace("Handler completed");
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error processing message {key} by {handler}", key, sub.GetType().Name);
        }
    }
}