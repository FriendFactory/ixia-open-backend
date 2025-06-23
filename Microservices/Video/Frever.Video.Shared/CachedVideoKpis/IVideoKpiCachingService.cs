using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Frever.Shared.MainDb.Entities;

namespace Frever.Videos.Shared.CachedVideoKpis;

public interface IVideoKpiCachingService
{
    Task<Dictionary<long, VideoKpi>> GetVideosKpis(long[] videoIds);

    Task UpdateVideoKpi(long videoId, Expression<Func<VideoKpi, long>> prop, int by);

    Task AddVideoLike(long videoId);

    Task RemoveVideoLike(long videoId);
}