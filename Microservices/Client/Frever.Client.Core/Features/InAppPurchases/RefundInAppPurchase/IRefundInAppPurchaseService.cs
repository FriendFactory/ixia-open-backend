using System;
using System.Linq;
using System.Threading.Tasks;
using Frever.ClientService.Contract.Common;
using Frever.ClientService.Contract.InAppPurchases;

namespace Frever.Client.Core.Features.InAppPurchases.RefundInAppPurchase;

public interface IRefundInAppPurchaseService
{
    Task RefundInAppPurchase(RefundInAppPurchaseRequest request);

    IQueryable<InAppPurchaseOrderInfo> GetNotRefundOrders(Platform platform, string[] storeOrderIdentifiers);
}

public class InAppPurchaseOrderInfo
{
    public string Platform { get; set; }

    public Guid Id { get; set; }

    public string StoreOrderIdentifier { get; set; }
}