using Common.Infrastructure.Caching.CacheKeys;

namespace Frever.Client.Shared.CommercialMusic;

public static class CommercialMusicCacheKeys
{
    public static readonly string Channel = "rpc::commercial-music::delete-content".FreverVersionedCache();
}