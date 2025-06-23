using Common.Infrastructure.Caching.CacheKeys;

namespace NotificationService.Client;

public static class NotificationSubscriptionKeys
{
    public static readonly string SubscriptionKey = "frever::notification".FreverVersionedCache();
}