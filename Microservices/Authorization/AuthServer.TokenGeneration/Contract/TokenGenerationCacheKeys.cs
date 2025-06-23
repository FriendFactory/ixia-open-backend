using System;
using Common.Infrastructure.Caching.CacheKeys;

namespace AuthServer.TokenGeneration.Contract;

public static class TokenGenerationCacheKeys
{
    public static string RequestChannel = "token-generation::req".FreverVersionedCache();
    public static string ResponseChannel = "token-generation::res".FreverVersionedCache();

    public static string LockCorrelationKey(Guid correlationId)
    {
        return $"token-generation::lock::{correlationId}".FreverVersionedCache();
    }
}