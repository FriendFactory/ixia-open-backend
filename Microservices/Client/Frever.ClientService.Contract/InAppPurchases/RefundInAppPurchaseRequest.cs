using Frever.ClientService.Contract.Common;

namespace Frever.ClientService.Contract.InAppPurchases;

public class RefundInAppPurchaseRequest
{
    public Platform Platform { get; set; }

    public string StoreOrderIdentifier { get; set; }
}