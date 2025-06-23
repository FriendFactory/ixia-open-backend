using Frever.Shared.MainDb.Entities;

namespace Frever.Shared.AssetStore.Transactions;

public class AssetToPurchase
{
    public long? AssetOfferId { get; set; }

    public AssetStoreAssetType AssetType { get; set; }

    public long AssetId { get; set; }

    public int? HardCurrencyPrice { get; set; }

    public int? SoftCurrencyPrice { get; set; }
}