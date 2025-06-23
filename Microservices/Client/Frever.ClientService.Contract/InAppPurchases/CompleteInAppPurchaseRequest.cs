using System;
using Frever.ClientService.Contract.Common;

namespace Frever.ClientService.Contract.InAppPurchases;

public class CompleteInAppPurchaseRequest
{
    public Guid PendingOrderId { get; set; }

    public Platform Platform { get; set; }

    public string TransactionData { get; set; }
}