using System.Threading.Tasks;

namespace Frever.Cache.PubSub;

public interface IPubSubPublisher
{
    Task Publish(string key, object message);
}