using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Infrastructure.Caching.CacheKeys;
using StackExchange.Redis;
using Order = StackExchange.Redis.Order;

namespace Frever.Client.Core.Features.AI.Generation.StatusUpdating;

public class PollingJob
{
    public const string PixVerse = "pix-verse";
    public const string Image = "image";
    public const string Video = "video";

    public long ContentId { get; set; }
    public string ContentType { get; set; }
    public string ResultKey { get; set; }
    public long GroupId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastPolled { get; set; } = DateTime.MinValue;
    public int PollAttempts { get; set; }
    public DateTime NextPollTime { get; set; } = DateTime.UtcNow;
    public PollingJobStatus Status { get; set; } = PollingJobStatus.InProgress;
}

public enum PollingJobStatus
{
    InProgress,
    Completed,
    Failed,
    TimedOut
}

public interface IPollingJobManager
{
    Task EnqueueJobAsync(long jobId, string contentType, string resultKey, long groupId);
    Task<List<PollingJob>> GetJobsDueForPolling(int batchSize = 10);
    Task UpdateJobStatus(long jobId, PollingJobStatus status, DateTime? nextPollTimeUtc = null);
    Task IncrementPollAttempts(long jobId, DateTime lastPolledUtc, DateTime nextPollTimeUtc);
    Task MarkJobAsCompleted(long jobId);
    Task MarkJobAsFailed(long jobId);
    Task MarkJobAsTimedOut(long jobId);
    Task<bool> AcquireLock(long jobId, string instanceId);
    Task ReleaseLock(long jobId);
    Task<PollingJob> GetJob(long jobId);
}

public class RedisPollingJobManager(IConnectionMultiplexer redis) : IPollingJobManager
{
    private readonly IDatabase _db = redis.GetDatabase();
    private TimeSpan LockExpiry { get; } = TimeSpan.FromSeconds(15);

    public async Task EnqueueJobAsync(long contentId, string contentType, string resultKey, long groupId)
    {
        var hashEntries = new HashEntry[]
                          {
                              new(nameof(PollingJob.ContentId), contentId),
                              new(nameof(PollingJob.ContentType), contentType),
                              new(nameof(PollingJob.ResultKey), resultKey),
                              new(nameof(PollingJob.GroupId), groupId),
                              new(nameof(PollingJob.CreatedAt), DateTime.UtcNow.Ticks),
                              new(nameof(PollingJob.Status), (int) PollingJobStatus.InProgress),
                              new(nameof(PollingJob.PollAttempts), 0),
                              new(nameof(PollingJob.LastPolled), DateTime.MinValue.Ticks),
                              new(nameof(PollingJob.NextPollTime), DateTime.UtcNow.Ticks)
                          };
        var transaction = _db.CreateTransaction();
        var jobHashKey = AiContentCacheKeys.JobHashKey(contentId);
        await _db.HashSetAsync(jobHashKey, hashEntries);
        await _db.SortedSetAddAsync(AiContentCacheKeys.PendingJobsSortedSetKey, contentId.ToString(), DateTime.UtcNow.Ticks);
        await _db.KeyExpireAsync(jobHashKey, TimeSpan.FromHours(1));
        await transaction.ExecuteAsync();
    }

    public async Task<List<PollingJob>> GetJobsDueForPolling(int batchSize = 10)
    {
        var jobIds = await _db.SortedSetRangeByScoreAsync(
                         AiContentCacheKeys.PendingJobsSortedSetKey,
                         0,
                         DateTime.UtcNow.Ticks,
                         Exclude.None,
                         Order.Ascending,
                         0,
                         batchSize
                     );

        if (jobIds.Length == 0)
            return [];

        var jobs = new List<PollingJob>();
        foreach (var jobId in jobIds)
        {
            var hashEntries = await _db.HashGetAllAsync(AiContentCacheKeys.JobHashKey((long) jobId));
            if (hashEntries is {Length: > 0})
                jobs.Add(MapHashEntriesToPollingJob(hashEntries));
            else
                await _db.SortedSetRemoveAsync(AiContentCacheKeys.PendingJobsSortedSetKey, jobId);
        }

        return jobs;
    }

    private static PollingJob MapHashEntriesToPollingJob(HashEntry[] hashEntries)
    {
        var dictionary = hashEntries.ToDictionary(h => h.Name.ToString(), h => h.Value);

        return new PollingJob
               {
                   ContentId = long.Parse(dictionary[nameof(PollingJob.ContentId)]),
                   ContentType = dictionary[nameof(PollingJob.ContentType)],
                   ResultKey = dictionary[nameof(PollingJob.ResultKey)],
                   GroupId = long.Parse(dictionary[nameof(PollingJob.GroupId)]),
                   CreatedAt = new DateTime(long.Parse(dictionary[nameof(PollingJob.CreatedAt)]), DateTimeKind.Utc),
                   Status = (PollingJobStatus) int.Parse(dictionary[nameof(PollingJob.Status)]),
                   PollAttempts = int.Parse(dictionary[nameof(PollingJob.PollAttempts)]),
                   LastPolled = new DateTime(long.Parse(dictionary[nameof(PollingJob.LastPolled)]), DateTimeKind.Utc),
                   NextPollTime = new DateTime(long.Parse(dictionary[nameof(PollingJob.NextPollTime)]), DateTimeKind.Utc)
               };
    }

    public async Task<PollingJob> GetJob(long jobId)
    {
        var jobHashKey = AiContentCacheKeys.JobHashKey(jobId);
        var hashEntries = await _db.HashGetAllAsync(jobHashKey);
        return hashEntries.Length == 0 ? null : MapHashEntriesToPollingJob(hashEntries);
    }

    public async Task UpdateJobStatus(long jobId, PollingJobStatus status, DateTime? nextPollTimeUtc = null)
    {
        var jobHashKey = AiContentCacheKeys.JobHashKey(jobId);
        var transaction = _db.CreateTransaction();

        await _db.HashSetAsync(jobHashKey, nameof(PollingJob.Status), (int) status);
        if (nextPollTimeUtc.HasValue)
            await _db.HashSetAsync(jobHashKey, nameof(PollingJob.NextPollTime), nextPollTimeUtc.Value.Ticks);

        if (status is PollingJobStatus.Completed or PollingJobStatus.Failed or PollingJobStatus.TimedOut)
        {
            await _db.SortedSetRemoveAsync(AiContentCacheKeys.PendingJobsSortedSetKey, jobId.ToString());
            await _db.KeyDeleteAsync(jobHashKey);
        }

        await transaction.ExecuteAsync();
    }

    public async Task IncrementPollAttempts(long jobId, DateTime lastPolledUtc, DateTime nextPollTimeUtc)
    {
        var jobHashKey = AiContentCacheKeys.JobHashKey(jobId);

        var transaction = _db.CreateTransaction();
        await _db.HashIncrementAsync(jobHashKey, nameof(PollingJob.PollAttempts));
        await _db.HashSetAsync(jobHashKey, nameof(PollingJob.LastPolled), lastPolledUtc.Ticks);
        await _db.HashSetAsync(jobHashKey, nameof(PollingJob.NextPollTime), nextPollTimeUtc.Ticks);
        await _db.SortedSetAddAsync(AiContentCacheKeys.PendingJobsSortedSetKey, jobId.ToString(), nextPollTimeUtc.Ticks);
        await transaction.ExecuteAsync();
    }

    public async Task MarkJobAsCompleted(long jobId)
    {
        var jobHashKey = AiContentCacheKeys.JobHashKey(jobId);

        var transaction = _db.CreateTransaction();
        await _db.SortedSetRemoveAsync(AiContentCacheKeys.PendingJobsSortedSetKey, jobId.ToString());
        await _db.KeyDeleteAsync(jobHashKey);
        await transaction.ExecuteAsync();
    }

    public async Task MarkJobAsFailed(long jobId)
    {
        var jobHashKey = AiContentCacheKeys.JobHashKey(jobId);

        var transaction = _db.CreateTransaction();
        await _db.SortedSetRemoveAsync(AiContentCacheKeys.PendingJobsSortedSetKey, jobId.ToString());
        await _db.KeyDeleteAsync(jobHashKey);
        await transaction.ExecuteAsync();
    }

    public async Task MarkJobAsTimedOut(long jobId)
    {
        var jobHashKey = AiContentCacheKeys.JobHashKey(jobId);

        var transaction = _db.CreateTransaction();
        await _db.SortedSetRemoveAsync(AiContentCacheKeys.PendingJobsSortedSetKey, jobId.ToString());
        await _db.KeyDeleteAsync(jobHashKey);
        await transaction.ExecuteAsync();
    }

    public async Task<bool> AcquireLock(long jobId, string instanceId)
    {
        var jobHashKey = AiContentCacheKeys.LockKey(jobId);
        var acquired = await _db.StringSetAsync(jobHashKey, instanceId, LockExpiry, When.NotExists);
        return acquired;
    }

    public async Task ReleaseLock(long jobId)
    {
        var jobHashKey = AiContentCacheKeys.LockKey(jobId);
        await _db.KeyDeleteAsync(jobHashKey);
    }
}