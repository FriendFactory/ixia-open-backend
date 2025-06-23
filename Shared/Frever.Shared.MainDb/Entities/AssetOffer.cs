using System;
using System.Collections.Generic;

namespace Frever.Shared.MainDb.Entities;

public class AssetOffer
{
    public AssetOffer()
    {
        AssetStoreTransactions = new HashSet<AssetStoreTransaction>();
    }

    public long Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public int? SoftCurrencyPrice { get; set; }
    public int? HardCurrencyPrice { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedTime { get; set; }
    public long CreatedByGroupId { get; set; }
    public long? ArchivedByGroupId { get; set; }
    public DateTime? ArchivedTime { get; set; }
    public DateTime? PublicationDate { get; set; }
    public DateTime? DepublicationDate { get; set; }
    public int? SortOrder { get; set; }
    public int? Discount { get; set; }
    public long? OutfitId { get; set; }
    public bool IsLevelPurchase { get; set; }

    public virtual ICollection<AssetStoreTransaction> AssetStoreTransactions { get; set; }
}