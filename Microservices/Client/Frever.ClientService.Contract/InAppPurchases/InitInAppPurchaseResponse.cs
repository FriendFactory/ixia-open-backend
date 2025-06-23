using System;

namespace Frever.ClientService.Contract.InAppPurchases;

public class InitInAppPurchaseResponse
{
    public Guid PendingOrderId { get; set; }

    public bool Ok { get; set; }

    public string ErrorCode { get; set; }

    public string ErrorMessage { get; set; }
}