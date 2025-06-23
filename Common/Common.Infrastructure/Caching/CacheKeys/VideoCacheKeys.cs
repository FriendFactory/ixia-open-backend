namespace Common.Infrastructure.Caching.CacheKeys;

public static class VideoCacheKeys
{
    public static readonly string PublicPrefix = "video-list::pub".FreverVersionedCache();
    public static readonly string SharedPrefix = "{video::shared}".FreverUnversionedCache();
    public static readonly string ConversionPrefix = "video-conversion".FreverVersionedCache();

    private static readonly string MlPoweredPersonalVideoFeedPrefix = $"{PublicPrefix}::personal-feed-v2";
    private static readonly string MlPoweredPersonalVideoFeedVersionPrefix = $"{PublicPrefix}::personal-feed-version-v2";

    public static string VideoInfoKey(long videoId)
    {
        return $"{{video-details}}/{videoId}".FreverVersionedCache();
    }

    public static string VideoKpiKey(long videoId)
    {
        return $"{{video-kpi}}/{videoId}".FreverUnversionedCache();
    }

    public static string MlPoweredPersonalVideoFeed(long groupId, int version)
    {
        return $"{MlPoweredPersonalVideoFeedPrefix}::{groupId}::{version}";
    }

    public static string MlPoweredPersonalVideoFeedVersion(long groupId)
    {
        return $"{MlPoweredPersonalVideoFeedVersionPrefix}::{groupId}::version";
    }

    public static string SharedVideo(string videoKey)
    {
        return $"{SharedPrefix}::{videoKey}";
    }

    public static string VideoJobWatchingSuspendedKey()
    {
        return $"{ConversionPrefix}::job-polling::video-processing-complete";
    }
}