namespace Common.Infrastructure.Caching.CacheKeys;

public static class NotificationCacheKeys
{
    public const string NotificationPerInstancePrefix = "frever::notification::per-instance";

    public static string NotificationPerInstanceKey(string key)
    {
        return $"{NotificationPerInstancePrefix}/{key}".FreverVersionedCache();
    }
}