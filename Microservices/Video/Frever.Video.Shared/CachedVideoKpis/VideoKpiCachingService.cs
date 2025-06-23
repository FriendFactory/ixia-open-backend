using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Common.Infrastructure.Caching.CacheKeys;
using Frever.Cache;
using Frever.Shared.MainDb.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Frever.Videos.Shared.CachedVideoKpis;

internal sealed class VideoKpiCachingService : IVideoKpiCachingService
{
    private readonly ILogger _log;
    private readonly IVideoKpiRepository _repo;
    private readonly IHashCache<VideoKpi> _videoKpiCache;

    public VideoKpiCachingService(ILoggerFactory loggerFactory, IVideoKpiRepository repo, IHashCache<VideoKpi> videoKpiCache)
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);

        _repo = repo ?? throw new ArgumentNullException(nameof(repo));
        _videoKpiCache = videoKpiCache ?? throw new ArgumentNullException(nameof(videoKpiCache));
        _log = loggerFactory.CreateLogger("Frever.Videos.VideoKpiService");
    }

    public async Task<Dictionary<long, VideoKpi>> GetVideosKpis(long[] videoIds)
    {
        ArgumentNullException.ThrowIfNull(videoIds);

        videoIds = videoIds.Distinct().ToArray();

        _log.LogInformation("VideoKpiService::GetCachedVideoKpi(videoIds:[{VideoIds}])", string.Join(",", videoIds));

        return await _videoKpiCache.GetOrCacheMany(
                   videoIds,
                   VideoCacheKeys.VideoKpiKey,
                   ids => _repo.GetVideoKpi(ids).ToDictionaryAsync(k => k.VideoId),
                   TimeSpan.FromDays(1)
               );
    }

    public async Task UpdateVideoKpi(long videoId, Expression<Func<VideoKpi, long>> prop, int by)
    {
        ArgumentNullException.ThrowIfNull(prop);

        var key = VideoCacheKeys.VideoKpiKey(videoId);

        await _videoKpiCache.Increment(key, prop, by);
    }

    public async Task AddVideoLike(long videoId)
    {
        await UpdateVideoKpi(videoId, kpi => kpi.Likes, 1);
    }

    public async Task RemoveVideoLike(long videoId)
    {
        await UpdateVideoKpi(videoId, kpi => kpi.Likes, -1);
    }
}