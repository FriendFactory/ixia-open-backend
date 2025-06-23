using System;
using System.Linq;
using System.Threading.Tasks;
using Frever.Shared.MainDb;
using Frever.Shared.MainDb.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Frever.Client.Core.Features.InAppPurchases.RefundInAppPurchase;

public class PersistentRefundRepository : IRefundRepository
{
    private readonly IWriteDb _mainDb;

    public PersistentRefundRepository(IWriteDb mainDb)
    {
        _mainDb = mainDb ?? throw new ArgumentNullException(nameof(mainDb));
    }

    public Task<IDbContextTransaction> BeginTransaction()
    {
        return _mainDb.BeginTransaction();
    }

    public IQueryable<InAppProduct> GetAllInAppProducts()
    {
        return _mainDb.InAppProduct;
    }

    public IQueryable<InAppProductDetails> GetAllInAppProductDetails(long inAppProductId)
    {
        return _mainDb.InAppProductDetails.Where(d => d.InAppProductId == inAppProductId);
    }

    public IQueryable<InAppPurchaseOrder> GetNonRefundOrders(string[] storeOrderIdentifiers)
    {
        return _mainDb.InAppPurchaseOrder.Where(o => !o.IsPending && !o.WasRefund)
                      .Where(o => o.StoreOrderIdentifier != null)
                      .Where(o => storeOrderIdentifiers.Contains(o.StoreOrderIdentifier));
    }
}