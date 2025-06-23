using System;
using System.Linq;
using Frever.Shared.MainDb;
using Frever.Shared.MainDb.Entities;
using Microsoft.EntityFrameworkCore;

namespace Frever.Videos.Shared.CachedVideoKpis;

public interface IVideoKpiRepository
{
    IQueryable<VideoKpi> GetVideoKpi(params long[] ids);
}

public class VideoKpiRepository(IReadDb db) : IVideoKpiRepository
{
    private readonly IReadDb _db = db ?? throw new ArgumentNullException(nameof(db));

    public IQueryable<VideoKpi> GetVideoKpi(params long[] ids)
    {
        ArgumentNullException.ThrowIfNull(ids);

        return _db.VideoKpi.Where(kpi => ids.Contains(kpi.VideoId)).AsNoTracking();
    }
}