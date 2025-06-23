using System;
using System.Linq;
using System.Threading.Tasks;
using Frever.Shared.MainDb;
using Frever.Shared.MainDb.Entities;

namespace Frever.Client.Core.Features.InAppPurchases.DataAccess;

public interface IPurchaseOrderRepository
{
    IQueryable<InAppPurchaseOrder> GetCompletedOrderByStoreOrderIdentifier(string storeOrderIdentifier);

    IQueryable<InAppPurchaseOrder> GetUserOrders(long groupId);

    Task SaveChanges();
}

public class PersistentPurchaseOrderRepository(IWriteDb db) : IPurchaseOrderRepository
{
    public IQueryable<InAppPurchaseOrder> GetCompletedOrderByStoreOrderIdentifier(string storeOrderIdentifier)
    {
        if (string.IsNullOrWhiteSpace(storeOrderIdentifier))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(storeOrderIdentifier));

        return db.InAppPurchaseOrder.Where(o => !o.IsPending).Where(o => o.StoreOrderIdentifier == storeOrderIdentifier);
    }

    public IQueryable<InAppPurchaseOrder> GetUserOrders(long groupId)
    {
        return db.InAppPurchaseOrder.Where(o => o.GroupId == groupId);
    }

    public Task SaveChanges()
    {
        return db.SaveChangesAsync();
    }
}