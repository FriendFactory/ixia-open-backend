using System;
using System.Threading.Tasks;
using Frever.Protobuf;
using StackExchange.Redis;

namespace Frever.Cache.PubSub;

internal class RedisPubSubPublisher : IPubSubPublisher
{
    private readonly ISubscriber _subscriber;

    public RedisPubSubPublisher(IConnectionMultiplexer redis)
    {
        ArgumentNullException.ThrowIfNull(redis);

        _subscriber = redis.GetSubscriber();
    }

    public Task Publish(string key, object message)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(key));

        var data = ProtobufConvert.SerializeObject(message);

        _subscriber.Publish(key, data);

        return Task.CompletedTask;
    }
}