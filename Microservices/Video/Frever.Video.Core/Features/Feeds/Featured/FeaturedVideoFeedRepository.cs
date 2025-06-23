using System.Collections.Generic;
using System.Threading.Tasks;
using Frever.Shared.MainDb;
using Frever.Shared.MainDb.Entities;

namespace Frever.Video.Core.Features.Feeds.Featured;

public interface IFeaturedVideoFeedRepository
{
    Task<List<VideoWithSong>> GetFeaturedVideos(
        GeoCluster geoCluster,
        long currentGroupId,
        long target,
        int takeNext,
        int takePrevious
    );
}

public class PersistentFeaturedVideoFeedRepository(IReadDb db) : IFeaturedVideoFeedRepository
{
    public Task<List<VideoWithSong>> GetFeaturedVideos(
        GeoCluster geoCluster,
        long currentGroupId,
        long target,
        int takeNext,
        int takePrevious
    )
    {
        return db.GetFeaturedVideoIds(
            geoCluster,
            currentGroupId,
            target,
            takeNext,
            takePrevious
        );
    }
}