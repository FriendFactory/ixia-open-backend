using System;

namespace Frever.Cache;

public static class InAppPurchaseCacheKeys
{
    public static readonly string InAppProductInternal = "in-app-purchase::in-app-products-internal".FreverAssetCacheKey();

    public static string InAppProductOffers(long groupId, DateTime at)
    {
        return "in-app-purchase::offers".FreverAssetCacheKey().CachePerUser(groupId).CacheDaily(at);
    }

    public static string InAppProductOfferDebugInfo(long groupId, DateTime at)
    {
        return "in-app-purchase::offers-generation-debug-info".FreverAssetCacheKey().CachePerUser(groupId).CacheDaily(at);
    }
}