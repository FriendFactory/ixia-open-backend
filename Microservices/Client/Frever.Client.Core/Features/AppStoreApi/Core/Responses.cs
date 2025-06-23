using Newtonsoft.Json;

namespace Frever.Client.Core.Features.AppStoreApi.Core;

public class TransactionInfoResponse
{
    [JsonProperty("signedTransactionInfo")] public string SignedTransactionInfoJwt { get; set; }
}

public class SubscriptionStatusResponse
{
    [JsonProperty("environment")] public string Environment { get; set; }

    [JsonProperty("bundleId")] public string BundleId { get; set; }

    [JsonProperty("data")] public SubscriptionData[] SubscriptionData { get; set; }
}

public class SubscriptionData
{
    [JsonProperty("subscriptionGroupIdentifier")] public string SubscriptionGroupId { get; set; }
    [JsonProperty("lastTransactions")] public SubscriptionTransactionInfo[] LastTransactions { get; set; }
}

public class SubscriptionTransactionInfo
{
    [JsonProperty("originalTransactionId")] public string OriginalTransactionId { get; set; }
    [JsonProperty("status")] public int Status { get; set; }
    [JsonProperty("signedTransactionInfo")] public string SignedTransactionInfo { get; set; }
    [JsonProperty("signedRenewalInfo")] public string SignedRenewalInfo { get; set; }
}

public class JwtTransactionInfo
{
    public const string KnownTypeConsumable = "Consumable";
    public const string KnownTypeNonConsumable = "Non-Consumable";
    public const string KnownTypeAutoRenewableSubscription = "Auto-Renewable Subscription";
    public const string KnownTypeNonRenewingSubscription = "Non-Renewing Subscription";

    public const string KnownOwnershipTypePurchased = "PURCHASED";
    public const string KnownOwnershipTypeFamilyShared = "FAMILY_SHARED";


    [JsonProperty("transactionId")] public string TransactionId { get; set; }
    [JsonProperty("originalTransactionId")] public string OriginalTransactionId { get; set; }
    [JsonProperty("bundleId")] public string BundleId { get; set; }
    [JsonProperty("productId")] public string ProductId { get; set; }
    [JsonProperty("purchaseDate")] public long PurchaseDate { get; set; }
    [JsonProperty("originalPurchaseDate")] public long OriginalPurchaseDate { get; set; }
    [JsonProperty("quantity")] public int Quantity { get; set; }
    [JsonProperty("type")] public string Type { get; set; }
    [JsonProperty("inAppOwnershipType")] public string InAppOwnershipType { get; set; }
    [JsonProperty("environment")] public string Environment { get; set; }

    [JsonProperty("transactionReason")] public string TransactionReason { get; set; }
    [JsonProperty("currency")] public string Currency { get; set; }
    [JsonProperty("price")] public int Price { get; set; }

    /// <summary>
    /// For subscriptions only
    /// </summary>
    [JsonProperty("expiresDate")]
    public long ExpiresDate { get; set; }

    [JsonProperty("isUpgraded")] public bool IsUpgraded { get; set; }

    [JsonProperty("revocationDate")] public long RevocationDate { get; set; }
    [JsonProperty("revocationReason")] public string RevocationReason { get; set; }
}

public class JwtSubscriptionRenewalInfo
{
    public const int KnownAutoRenewStatusOn = 1;
    public const int KnownAutoRenewStatusOff = 0;

    [JsonProperty("expirationIntent")] public int ExpirationIntent { get; set; }
    [JsonProperty("originalTransactionId")] public string OriginalTransactionId { get; set; }
    [JsonProperty("autoRenewProductId")] public string AutoRenewProductId { get; set; }
    [JsonProperty("productId")] public string ProductId { get; set; }
    [JsonProperty("autoRenewStatus")] public int AutoRenewStatus { get; set; }
    [JsonProperty("isInBillingRetryPeriod")] public bool IsInBillingRetryPeriod { get; set; }
    [JsonProperty("recentSubscriptionStartDate")] public long RecentSubscriptionStartDate { get; set; }
    [JsonProperty("renewalDate")] public long RenewalDate { get; set; }
    [JsonProperty("environment")] public string Environment { get; set; }
}