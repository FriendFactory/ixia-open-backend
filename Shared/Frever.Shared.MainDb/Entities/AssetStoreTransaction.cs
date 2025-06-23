using System;
using System.Collections.Generic;

namespace Frever.Shared.MainDb.Entities;

public class AssetStoreTransaction
{
    public AssetStoreTransaction()
    {
        AssetStoreTransactionAssets = new HashSet<AssetStoreTransactionAsset>();
    }

    public long Id { get; set; }
    public long GroupId { get; set; }
    public DateTime CreatedTime { get; set; } = DateTime.UtcNow;
    public AssetStoreTransactionType TransactionType { get; set; }
    public int SoftCurrencyAmount { get; set; }
    public int HardCurrencyAmount { get; set; }
    public int UsdAmountCents { get; set; }
    public long? AssetOfferId { get; set; }
    public long? InAppProductId { get; set; }
    public string InAppPurchaseRef { get; set; }
    public long? HardCurrencyExchangeOfferId { get; set; }
    public long? EntityRefId { get; set; }
    public Guid TransactionGroup { get; set; }
    public long? UserActivityId { get; set; }

    public int? SoftCurrencyAmountNoDiscount { get; set; }
    public int? HardCurrencyAmountNoDiscount { get; set; }
    public string AiWorkflow { get; set; }
    public decimal? AiWorkflowBillingUnits { get; set; }

    public long? InAppUserSubscriptionId { get; set; }

    public virtual AssetOffer AssetOffer { get; set; }
    public virtual InAppProduct InAppProduct { get; set; }
    public virtual ICollection<AssetStoreTransactionAsset> AssetStoreTransactionAssets { get; set; }
}