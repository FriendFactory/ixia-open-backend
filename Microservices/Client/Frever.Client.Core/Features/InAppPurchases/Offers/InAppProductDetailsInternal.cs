using Frever.Shared.MainDb.Entities;

namespace Frever.Client.Core.Features.InAppPurchases.Offers;

public class InAppProductDetailsInternal
{
    public long Id { get; set; }
    public string Title { get; set; }
    public long? AssetId { get; set; }
    public AssetStoreAssetType? AssetType { get; set; }
    public int UniqueOfferGroup { get; set; }
    public int? HardCurrency { get; set; }
    public int? SoftCurrency { get; set; }
    public UsageEstimation UsageEstimation { get; set; }
    public override string ToString()
    {
        var productValue = "";
        if (AssetId != null)
            productValue = $"{AssetType} {AssetId}";
        else if (HardCurrency != null)
            productValue = $"{HardCurrency} HC";
        else if (SoftCurrency != null)
            productValue = $"{SoftCurrency} SC";

        return $"ID={Id} {productValue} {Title} (UOG={UniqueOfferGroup})";
    }
}