using Common.Models.Files;

namespace Frever.Shared.MainDb.Entities;

public class InAppProductDetails : IFileMetadataConfigRoot
{
    public long Id { get; set; }
    public long InAppProductId { get; set; }
    public long? AssetId { get; set; }
    public long SortOrder { get; set; }
    public AssetStoreAssetType? AssetType { get; set; }
    public int? HardCurrency { get; set; }
    public int? SoftCurrency { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public bool IsActive { get; set; }
    public int UniqueOfferGroup { get; set; }
    public FileMetadata[] Files { get; set; }
}

