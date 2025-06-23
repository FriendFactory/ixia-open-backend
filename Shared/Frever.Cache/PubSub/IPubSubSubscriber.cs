using System.Threading.Tasks;
using StackExchange.Redis;

namespace Frever.Cache.PubSub;

public interface IPubSubSubscriber
{
    string SubscriptionKey { get; }
    Task OnMessage(RedisValue message);
}