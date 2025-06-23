using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Frever.Shared.MainDb;
using Frever.Shared.MainDb.Entities;
using Microsoft.EntityFrameworkCore;

namespace Frever.Video.Core.Features.Feeds.Trending;

public interface ITrendingVideoFeedRepository
{
    Task<List<VideoWithSong>> GetTrendingVideo(
        GeoCluster geoCluster,
        long currentGroupId,
        int videosCount,
        long target,
        int takeNext
    );
}

public class PersistentTrendingVideoFeedRepository(IReadDb db) : ITrendingVideoFeedRepository
{
    public Task<List<VideoWithSong>> GetTrendingVideo(
        GeoCluster geoCluster,
        long currentGroupId,
        int videosCount,
        long target,
        int takeNext
    )
    {
        return db.GetTrendingVideoQuery(geoCluster, currentGroupId, videosCount, target).Take(takeNext).ToListAsync();
    }
}