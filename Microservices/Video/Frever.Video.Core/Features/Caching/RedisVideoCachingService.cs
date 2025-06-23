using System.Threading.Tasks;
using Common.Infrastructure.Caching;
using Common.Infrastructure.Caching.CacheKeys;

#pragma warning disable CS8619, CS8603

namespace Frever.Video.Core.Features.Caching;

internal sealed class RedisVideoCachingService(ICache cache) : IVideoCachingService
{
    public Task DeleteVideoDetailsCache(long videoId)
    {
        var keys = VideoCacheKeys.VideoInfoKey(videoId).AllKeyVersionedCache();

        return cache.DeleteKeys(keys);
    }
}