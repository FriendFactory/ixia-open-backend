using System;
using System.Threading.Tasks;
using Common.Infrastructure.Caching;
using Frever.Cache;
using Newtonsoft.Json.Linq;
using StackExchange.Redis;

namespace Frever.AdminService.Core.Services.InAppPurchases.OfferGenerationInfo;

public class InAppOfferGenerationInfoService(ICache cache) : IInAppOfferGenerationInfoService
{
    private readonly ICache _cache = cache ?? throw new ArgumentNullException(nameof(cache));

    public async Task<JObject> GetOfferGenerationDebugInfo(long groupId)
    {
        var data = await _cache.Db().StringGetAsync(InAppPurchaseCacheKeys.InAppProductOfferDebugInfo(groupId, DateTime.UtcNow));

        if (data == RedisValue.Null)
            return null;

        var info = JObject.Parse(data);

        var result = new JObject {{"debugInfo", info}};

        var offerString = await _cache.Db().StringGetAsync(InAppPurchaseCacheKeys.InAppProductOffers(groupId, DateTime.UtcNow));
        if (offerString == RedisValue.Null)
            return result;

        var offer = JObject.Parse(offerString);
        result.Add("offer", offer);

        return result;
    }
}