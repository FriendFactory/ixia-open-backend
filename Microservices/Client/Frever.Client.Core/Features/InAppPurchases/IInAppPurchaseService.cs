using System;
using System.Threading.Tasks;
using Frever.ClientService.Contract.Common;
using Frever.ClientService.Contract.InAppPurchases;

namespace Frever.Client.Core.Features.InAppPurchases;

public interface IInAppPurchaseService
{
    Task<InitInAppPurchaseResponse> InitInAppPurchase(InitInAppPurchaseRequest request);

    Task<CompleteInAppPurchaseResponse> CompleteInAppPurchase(CompleteInAppPurchaseRequest request);

    Task CancelAllSubscriptions();
}

public class RestoreInAppPurchaseRequest
{
    public required Platform Platform { get; set; }

    /// <summary>
    /// Gets or sets optional transaction ID used if user has no purchases yet.
    /// </summary>
    public string TransactionId { get; set; }
}

public class RestoreInAppPurchaseResult
{
    public required bool Ok { get; set; }
    public string ErrorMessage { get; set; }

    public int? PermanentTokenRestored { get; set; }
    public string SubscriptionRestored { get; set; }

    public InAppPurchaseRestoreDetails[] Details { get; set; } = [];
}

public class InAppPurchaseRestoreDetails
{
    public required string TransactionId { get; set; }
    public required Guid? InAppPurchaseOrderId { get; set; }
    public required string StoreProductRef { get; set; }
    public required long? InAppProductId { get; set; }
    public required string InAppProductTitle { get; set; }
    public required int HardCurrency { get; set; }
    public required bool IsSubscription { get; set; }
    public required bool WasRestored { get; set; }
    public required bool IsFromAnotherAccount { get; set; }

    public required string ErrorMessage { get; set; }
}