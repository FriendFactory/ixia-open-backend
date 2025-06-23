using System;
using System.Collections.Generic;
using Common.Models.Files;

namespace Frever.Shared.MainDb.Entities;

public class InAppProduct : IFileMetadataConfigRoot
{
    public InAppProduct()
    {
        AssetStoreTransactions = new HashSet<AssetStoreTransaction>();
    }

    public long Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string AppStoreProductRef { get; set; }
    public string PlayMarketProductRef { get; set; }
    public bool IsActive { get; set; }
    public bool IsSeasonPass { get; set; }
    public long? InAppProductPriceTierId { get; set; }
    public long SortOrder { get; set; }
    public DateTime? PublicationDate { get; set; }
    public DateTime? DepublicationDate { get; set; }
    public bool IsFreeProduct { get; set; }
    public bool IsSubscription { get; set; }
    public bool IsPopular { get; set; }
    public int DailyHardCurrency { get; set; }
    public int MonthlyHardCurrency { get; set; }
    public FileMetadata[] Files { get; set; }
    public UsageEstimation UsageEstimation { get; set; }

    public virtual ICollection<AssetStoreTransaction> AssetStoreTransactions { get; set; }
}

public class UsageEstimation
{
    public int ImageGenerationCount { get; set; }
    public int VideoGenerationCount { get; set; }
    public int SoundGenerationCount { get; set; }
}