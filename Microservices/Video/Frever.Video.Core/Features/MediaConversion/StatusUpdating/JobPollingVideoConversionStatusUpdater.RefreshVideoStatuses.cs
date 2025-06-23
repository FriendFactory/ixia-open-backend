using System;
using System.Linq;
using System.Threading.Tasks;
using Common.Infrastructure.Caching.CacheKeys;
using Frever.Shared.MainDb.Entities;
using Frever.Video.Core.Features.MediaConversion.DataAccess;
using Frever.Video.Core.Features.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Frever.Video.Core.Features.MediaConversion.StatusUpdating;

public partial class JobPollingVideoConversionStatusUpdater
{
    private static string LockRefreshVideoStatusesRedisKey => "frever::video-conversion-status::refresh-video::lock".FreverVersionedCache();

    private async Task RefreshVideoStatusesOnce()
    {
        if (!await IsVideoProcessingActive())
        {
            _logger.LogDebug("Video processing is not active");
            return;
        }

        if (!_redis.LockTake(LockRefreshVideoStatusesRedisKey, true, TimeSpan.FromSeconds(20)))
        {
            _logger.LogDebug("Video statuses is already refreshing");
            return;
        }

        try
        {
            using var services = _serviceProvider.CreateScope();

            var statusUpdateService = CreateStatusUpdateService(services);
            var repo = services.ServiceProvider.GetRequiredService<IVideoStatusUpdateRepository>();

            var notBefore = DateTime.UtcNow - Recent;
            var nonConvertedVideos = await repo.GetRecentNonConvertedVideos(notBefore)
                                               .Select(v => new {v.Id, v.ConversionStatus, v.CreatedTime})
                                               .ToArrayAsync();

            _logger.LogDebug("{n} non-converted videos after {t}", nonConvertedVideos.Length, notBefore);

            if (!nonConvertedVideos.Any())
            {
                _logger.LogInformation("All videos processed");
                await StopVideoProcessing();
            }

            var jobs = await _awsConversionSnapshot.GetCachedData(nonConvertedVideos.Select(a => a.Id).ToHashSet().ToArray());

            foreach (var video in nonConvertedVideos)
            {
                _logger.LogDebug("Processing video {vid} status {s}", video.Id, video.ConversionStatus);

                if (jobs.TryGetValue(video.Id, out var status))
                {
                    _logger.LogDebug("Job for video {vid} found: {jobInfo}", video.Id, status.ToString());
                    if (status.HasError)
                    {
                        _logger.LogWarning("Video {vid} converted with error", video.Id);
                        continue;
                    }

                    if (status.IsThumbnailFileConverted && !video.ConversionStatus.HasFlag(VideoConversion.ThumbnailConverted))
                    {
                        await statusUpdateService.HandleVideoConversionCompletion(video.Id, VideoConversionType.Thumbnail);
                        _logger.LogInformation("Video {vid}: thumbnail converted", video.Id);
                    }

                    if (status.IsVideoFileConverted && !video.ConversionStatus.HasFlag(VideoConversion.VideoConverted))
                    {
                        await statusUpdateService.HandleVideoConversionCompletion(video.Id, VideoConversionType.Video);
                        _logger.LogInformation("Video {vid}: main file converted", video.Id);
                    }

                    if (status.IsThumbnailFileConverted && status.IsVideoFileConverted)
                        _logger.LogInformation(
                            "Video {vid} conversion completed, took {elpsd}",
                            video.Id,
                            DateTime.Now - video.CreatedTime
                        );
                }
            }
        }
        finally
        {
            _redis.LockRelease(LockRefreshVideoStatusesRedisKey, true);
        }
    }
}