using System.Linq;
using System.Threading.Tasks;
using Frever.Shared.MainDb.Entities;
using Microsoft.EntityFrameworkCore.Storage;

namespace Frever.Client.Core.Features.InAppPurchases.RefundInAppPurchase;

public interface IRefundRepository
{
    Task<IDbContextTransaction> BeginTransaction();

    IQueryable<InAppProduct> GetAllInAppProducts();

    IQueryable<InAppProductDetails> GetAllInAppProductDetails(long inAppProductId);

    IQueryable<InAppPurchaseOrder> GetNonRefundOrders(string[] storeOrderIdentifiers);
}