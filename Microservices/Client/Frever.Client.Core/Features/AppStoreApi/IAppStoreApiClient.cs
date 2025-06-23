using System;
using System.Threading.Tasks;

namespace Frever.Client.Core.Features.AppStoreApi;

public interface IAppStoreApiClient
{
    Task<AppStoreTransactionStatus> CheckAppStoreTransactionStatus(string transactionId);

    Task<SubscriptionStatus> CheckSubscriptionStatus(string transactionId);

    Task<AppStoreTransactionStatus[]> TransactionHistory(string anyTransactionId);

    Task<SubscriptionStatus> SubscriptionHistory(string anyTransactionId);
}

public class AppStoreTransactionStatus
{
    public bool IsValid { get; set; }

    public string BundleId { get; set; }

    public string TransactionId { get; set; }

    public string InAppProductId { get; set; }

    public string Environment { get; set; }

    public bool IsSubscription { get; set; }

    public bool IsRefunded { get; set; }

    public DateTime TransactionDate { get; set; }

    public string Currency { get; set; }
    public decimal Price { get; set; }
}

public class SubscriptionStatus
{
    public required bool IsSubscriptionActive { get; set; }

    public required SubscriptionTransactionData[] LastTransactions { get; set; } = [];
}

public class SubscriptionTransactionData
{
    public required bool IsActive { get; set; }
    public required int Status { get; set; }
    public required AppStoreTransactionStatus TransactionInfo { get; set; }
    public required SubscriptionRenewalInfo RenewalInfo { get; set; }
}

public class SubscriptionRenewalInfo
{
    public required string OriginalTransactionId { get; set; }
    public required string AutoRenewalProductId { get; set; }
    public required string ProductId { get; set; }
    public required bool IsInBillingRetryPeriod { get; set; }
    public required string Environment { get; set; }
    public required DateTimeOffset RecentSubscriptionStartDate { get; set; }
    public required DateTimeOffset RenewalDate { get; set; }
}