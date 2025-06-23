using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Infrastructure.Database;
using Frever.Shared.MainDb;
using Frever.Shared.MainDb.Entities;

namespace Frever.Client.Core.Features.InAppPurchases.Subscriptions.Data;

public interface ISubscriptionInfoRepository
{
    IQueryable<InAppUserSubscription> GetGroupSubscription(long groupId);

    IQueryable<InAppProduct> GetInAppProducts();

    IQueryable<AssetStoreTransaction> GetGroupTransactions(long groupId);

    IQueryable<InAppPurchaseOrder> GetInAppPurchaseOrder(Guid id);

    Task RecordAssetStoreTransactions(IEnumerable<AssetStoreTransaction> assetStoreTransactions);

    Task<NestedTransaction> BeginTransaction();
    
    void AddInAppUserSubscription(InAppUserSubscription subscription);

    Task SaveChanges();
}

public class PersistentSubscriptionInfoRepository(IWriteDb db) : ISubscriptionInfoRepository
{
    public IQueryable<InAppUserSubscription> GetGroupSubscription(long groupId)
    {
        return db.InAppUserSubscription.Where(s => s.GroupId == groupId);
    }

    public IQueryable<InAppProduct> GetInAppProducts()
    {
        return db.InAppProduct;
    }

    public IQueryable<AssetStoreTransaction> GetGroupTransactions(long groupId)
    {
        return db.AssetStoreTransactions.Where(t => t.GroupId == groupId);
    }

    public IQueryable<InAppPurchaseOrder> GetInAppPurchaseOrder(Guid id)
    {
        return db.InAppPurchaseOrder.Where(o => o.Id == id);
    }

    public void AddInAppUserSubscription(InAppUserSubscription subscription)
    {
        if (subscription == null)
            throw new ArgumentNullException(nameof(subscription));
        db.InAppUserSubscription.Add(subscription);
    }

    public Task SaveChanges()
    {
        return db.SaveChangesAsync();
    }

    public async Task RecordAssetStoreTransactions(IEnumerable<AssetStoreTransaction> assetStoreTransactions)
    {
        if (assetStoreTransactions == null)
            throw new ArgumentNullException(nameof(assetStoreTransactions));

        foreach (var t in assetStoreTransactions)
            db.AssetStoreTransactions.Add(t);

        await db.SaveChangesAsync();
    }

    public Task<NestedTransaction> BeginTransaction()
    {
        return db.BeginTransactionSafe();
    }
}