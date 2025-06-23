namespace Common.Infrastructure.Caching.CacheKeys;

public class AiContentCacheKeys
{
    public static readonly string AiContentGenerationPrefix = "ai-content-generation".FreverUnversionedCache();

    public static readonly string PendingJobsSortedSetKey = $"{AiContentGenerationPrefix}::pending-jobs";
    private static readonly string JobHashPrefix  = $"{AiContentGenerationPrefix}::job::";
    private static readonly string LockKeyPrefix = $"{AiContentGenerationPrefix}::lock::";
    private static readonly string DraftStatusPrefix = $"{AiContentGenerationPrefix}::draft-generation-status";

    public static string JobHashKey(long jobId)
    {
        return $"{JobHashPrefix}{jobId}";
    }

    public static string LockKey(long jobId)
    {
        return $"{LockKeyPrefix}{jobId}";
    }

    public static string DraftGenerationStatusKey(long id)
    {
        return DraftStatusPrefix + "::" + id;
    }
}