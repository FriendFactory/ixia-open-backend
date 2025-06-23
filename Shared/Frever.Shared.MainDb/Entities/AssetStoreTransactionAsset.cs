namespace Frever.Shared.MainDb.Entities;

public class AssetStoreTransactionAsset
{
    public long Id { get; set; }
    public long AssetStoreTransactionId { get; set; }
    public long AssetId { get; set; }
    public AssetStoreAssetType AssetType { get; set; }

    public AssetStoreTransaction AssetStoreTransaction { get; set; }
}