using System;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Frever.Client.Core.Features.AppStoreApi;

public abstract class VerifyReceiptRequest
{
    [JsonProperty("receipt-data")] public required string Receipt { get; set; }
    [JsonProperty("exclude-old-transactions")] public required bool ExcludeOldTransactions { get; set; }
}

public class VerifyProductReceiptRequest : VerifyReceiptRequest { }

public class VerifySubscriptionReceiptRequest : VerifyReceiptRequest
{
    [JsonProperty("password")] public required string SharedSecret { get; set; }
}

public class AppStoreInAppPurchaseReceipt
{
    [JsonProperty("environment")] public string Environment { get; set; }

    [JsonProperty("status")] public int Status { get; set; }

    [JsonProperty("receipt")] public ReceiptInfo Receipt { get; set; }

    [JsonProperty("pending_renewal_info")] public PendingRenewalInfo[] PendingRenewalInfo { get; set; }
}

public class ReceiptInfo
{
    [JsonProperty("bundle_id")] public string BundleId { get; set; }

    [JsonProperty("in_app")] public InAppProductInfo[] Products { get; set; }
}

public class InAppProductInfo
{
    [JsonProperty("product_id")] public string ProductId { get; set; }

    [JsonProperty("transaction_id")] public string TransactionId { get; set; }
    
    [JsonProperty("original_transaction_id")] public string OriginalTransactionId { get; set; }

    [JsonProperty("in_app_ownership_type")] public string OwnershipType { get; set; }
}

public class PendingRenewalInfo
{
    public const string KnownStatusRenewAutomatically = "1";
    public const string KnownStatusNoAutoRenew = "0";

    [JsonProperty("auto_renew_product_id")] public string AutoRenewProductId { get; set; }
    [JsonProperty("original_transaction_id")] public string OriginalTransactionId { get; set; }
    [JsonProperty("auto_renew_status")] public string AutoRenewStatus { get; set; }
}