using Frever.Common.IntegrationTesting;
using Frever.Shared.MainDb.Entities;

namespace Frever.Client.Core.IntegrationTest.Features.InAppPurchase.Data;

public static class InAppProductOfferData
{
    public static async Task<InAppProduct[]> WithInAppProducts(this DataEnvironment data, params InAppProductInput[] products)
    {
        ArgumentNullException.ThrowIfNull(data);

        var result = new List<InAppProduct>();
        foreach (var inAppProduct in products)
        {
            var entity = new InAppProduct
                         {
                             Files = [],
                             Title = inAppProduct.Title,
                             IsActive = true,
                             IsSeasonPass = inAppProduct.IsSeasonPass,
                             AppStoreProductRef = inAppProduct.AppleProductRef,
                             PlayMarketProductRef = inAppProduct.GoogleProductRef,
                             InAppProductPriceTierId = inAppProduct.IsFreeProduct ? default(long?) : 1,
                             IsFreeProduct = inAppProduct.IsFreeProduct,
                             IsSubscription = inAppProduct.IsSubscription,
                             DailyHardCurrency = inAppProduct.DailyHardCurrency,
                             MonthlyHardCurrency = inAppProduct.MonthlyHardCurrency
                         };
            
            result.Add(entity);
            data.Db.InAppProduct.Add(entity);
            await data.Db.SaveChangesAsync();

            await data.Db.InAppProductDetails.AddRangeAsync(
                inAppProduct.Details.Select(
                    d => new InAppProductDetails
                         {
                             Files = [],
                             Title = Guid.NewGuid().ToString("N"),
                             AssetId = d.AssetId,
                             AssetType = d.AssetType,
                             HardCurrency = d.HardCurrency,
                             SoftCurrency = d.SoftCurrency,
                             IsActive = true,
                             InAppProductId = entity.Id
                         }
                )
            );

            await data.Db.SaveChangesAsync();
        }

        return result.ToArray();
    }
}

public class InAppProductInput
{
    public string Title { get; set; }
    public string AppleProductRef { get; set; }
    public string GoogleProductRef { get; set; }
    public bool IsSeasonPass { get; set; }
    public bool IsFreeProduct { get; set; }
    public bool IsSubscription { get; set; }
    public int MonthlyHardCurrency { get; set; } = 1500;
    public int DailyHardCurrency { get; set; } = 30;

    public InAppProductDetailsInput[] Details { get; set; } = [];
}

public class InAppProductDetailsInput
{
    public AssetStoreAssetType? AssetType { get; set; }
    public long? AssetId { get; set; }
    public int? HardCurrency { get; set; }
    public int? SoftCurrency { get; set; }
}