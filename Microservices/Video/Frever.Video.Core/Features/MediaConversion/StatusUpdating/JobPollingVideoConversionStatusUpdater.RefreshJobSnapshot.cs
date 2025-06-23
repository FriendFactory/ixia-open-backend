using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.MediaConvert;
using Amazon.MediaConvert.Model;
using Common.Infrastructure.Caching.CacheKeys;
using Frever.Cache.Throttling;
using Frever.Video.Core.Features.MediaConversion.Client;
using Frever.Video.Core.Features.Shared;
using Microsoft.Extensions.Logging;

namespace Frever.Video.Core.Features.MediaConversion.StatusUpdating;

public partial class JobPollingVideoConversionStatusUpdater
{
    public static string MediaConvertJobStatusRedisKey()
    {
        return "frever::video-conversion-status::aws-job-status".FreverVersionedCache();
    }

    public static string LockMediaConvertJobStatusRedisKey()
    {
        return $"{MediaConvertJobStatusRedisKey()}::lock";
    }

    /// <summary>
    ///     Loads recent jobs from AWS media convert queue and update Redis cache snapshot.
    /// </summary>
    private async Task RefreshAwsConversionJobSnapshotOnce()
    {
        if (!await IsVideoProcessingActive())
        {
            _logger.LogDebug("Video processing is not active");
            return;
        }

        if (!_redis.LockTake(LockMediaConvertJobStatusRedisKey(), true, TimeSpan.FromSeconds(20)))
        {
            _logger.LogDebug("AWS Media queue is refreshing already");
            return;
        }

        try
        {
            var jobs = await LoadRecentMediaConvertJobs();

            foreach (var j in jobs)
                await _awsConversionSnapshot.AddOrUpdate(
                    j.VideoId,
                    Recent,
                    async existing =>
                    {
                        var result = existing == null ? new AwsMediaConvertJobCacheInfo() : existing.Copy();

                        result.HasError = result.HasError || j.HasError;
                        result.IsThumbnailFileConverted = result.IsThumbnailFileConverted ||
                                                          (j.ConversionType == VideoConversionType.Thumbnail && !j.HasError &&
                                                           !j.IsInProgress);
                        result.IsVideoFileConverted = result.IsVideoFileConverted ||
                                                      (j.ConversionType == VideoConversionType.Video && !j.HasError && !j.IsInProgress);

                        if (existing == null)
                            _logger.LogInformation("Job for video {vid} added", j.VideoId);

                        if (existing != null)
                        {
                            if (result.IsThumbnailFileConverted != existing.IsThumbnailFileConverted &&
                                j.ConversionType == VideoConversionType.Thumbnail)
                                _logger.LogInformation(
                                    "Video ID={id}: THUMBNAIL conversion took {exp} (submitted {sb}, started {st}, finished {f})",
                                    j.VideoId,
                                    j.Timing.SubmitTime - j.Timing.FinishTime,
                                    j.Timing.SubmitTime,
                                    j.Timing.StartTime,
                                    j.Timing.FinishTime
                                );

                            if (result.IsVideoFileConverted != existing.IsVideoFileConverted &&
                                j.ConversionType == VideoConversionType.Video)
                                _logger.LogInformation(
                                    "Video ID={id}: MAIN_FILE conversion took {exp} (submitted {sb}, started {st}, finished {f})",
                                    j.VideoId,
                                    j.Timing.SubmitTime - j.Timing.FinishTime,
                                    j.Timing.SubmitTime,
                                    j.Timing.StartTime,
                                    j.Timing.FinishTime
                                );
                        }

                        if (existing == null || !existing.Equals(result))
                            await StartVideoProcessing();

                        return result;
                    }
                );
        }
        finally
        {
            _redis.LockRelease(LockMediaConvertJobStatusRedisKey(), true);
        }
    }

    private async Task<AwsJobInfo[]> LoadRecentMediaConvertJobs()
    {
        _logger.LogDebug("Refresh AWS Media Convert job list");

        var awsJobs = new List<Job>();

        var nextPageToken = default(string);
        while (true)
        {
            var response = await _throttle.ThrottleAwsMediaConvert(
                               async () => await _awsMediaConvert.ListJobsAsync(
                                               new ListJobsRequest
                                               {
                                                   Order = Order.DESCENDING,
                                                   Queue = _config.MediaConvertQueue,
                                                   MaxResults = 20,
                                                   NextToken = nextPageToken
                                               }
                                           )
                           );

            _logger.LogDebug("Loaded page containing {cnt} item, next page token is {npt}", response.Jobs.Count, response.NextToken);


            var notBefore = DateTime.UtcNow - Recent;
            var recentJobs = response.Jobs.Where(j => j.Timing.SubmitTime >= notBefore);

            if (string.IsNullOrWhiteSpace(response.NextToken) || !recentJobs.Any())
            {
                _logger.LogDebug("List of jobs fully loaded");
                break;
            }

            nextPageToken = response.NextToken;
            awsJobs.AddRange(recentJobs);
        }

        _logger.LogDebug("Loaded {n} jobs from AWS", awsJobs.Count);

        return awsJobs.Select(ToAwsJobInfo).Where(v => v != null).ToArray();
    }

    private AwsJobInfo ToAwsJobInfo(Job awsJob)
    {
        var result = new AwsJobInfo();

        if (awsJob.UserMetadata.TryGetValue(ConversionJobMetadataHelper.JobMetadataVideoId, out var videoIdString))
            result.VideoId = long.Parse(videoIdString);
        else
            return null;

        if (!awsJob.UserMetadata.TryGetValue(ConversionJobMetadataHelper.JobMetadataConversionType, out var conversionTypeStr))
            return null;

        var conversionType = (VideoConversionType) Enum.Parse(typeof(VideoConversionType), conversionTypeStr, true);

        // Job is in progress
        result.IsInProgress = awsJob.Status == JobStatus.SUBMITTED || awsJob.Status == JobStatus.PROGRESSING;
        result.HasError = awsJob.Status == JobStatus.ERROR || awsJob.Status == JobStatus.CANCELED;
        result.ConversionType = conversionType;
        result.Timing = awsJob.Timing;

        return result;
    }


    private class AwsJobInfo
    {
        public long VideoId { get; set; }
        public VideoConversionType ConversionType { get; set; }
        public bool HasError { get; set; }
        public bool IsInProgress { get; set; }
        public Timing Timing { get; set; }
    }
}

public class AwsMediaConvertJobCacheInfo : IEquatable<AwsMediaConvertJobCacheInfo>
{
    public bool IsVideoFileConverted { get; set; }
    public bool IsThumbnailFileConverted { get; set; }

    public bool HasError { get; set; }


    public bool Equals(AwsMediaConvertJobCacheInfo other)
    {
        if (ReferenceEquals(null, other))
            return false;
        if (ReferenceEquals(this, other))
            return true;
        return IsVideoFileConverted == other.IsVideoFileConverted && IsThumbnailFileConverted == other.IsThumbnailFileConverted &&
               HasError == other.HasError;
    }

    public override string ToString()
    {
        return HasError ? "CONVERSION_ERROR" : $"Video={IsVideoFileConverted}; Thumb={IsThumbnailFileConverted}";
    }

    public AwsMediaConvertJobCacheInfo Copy()
    {
        return new AwsMediaConvertJobCacheInfo
               {
                   IsThumbnailFileConverted = IsThumbnailFileConverted, IsVideoFileConverted = IsVideoFileConverted, HasError = HasError
               };
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj))
            return false;
        if (ReferenceEquals(this, obj))
            return true;
        if (obj.GetType() != GetType())
            return false;
        return Equals((AwsMediaConvertJobCacheInfo) obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(IsVideoFileConverted, IsThumbnailFileConverted, HasError);
    }
}