using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Infrastructure.Database;
using Frever.Shared.MainDb;
using Frever.Shared.MainDb.Entities;

namespace Frever.Shared.AssetStore.DailyTokenRefill.DataAccess;

public class PersistentDailyTokenRefillRepository(IWriteDb db) : IDailyTokenRefillRepository
{
    public IQueryable<BalanceInfo> GetBalance(long[] groupIds)
    {
        return db.GetGroupBalanceInfo(groupIds);
    }

    public IQueryable<GroupActiveSubscriptionInfo> GetGroupActiveSubscription(bool excludeGroupsWithRefilledDailyTokens)
    {
        return db.GetGroupActiveSubscriptions(excludeGroupsWithRefilledDailyTokens);
    }

    public void AddTransactions(IEnumerable<AssetStoreTransaction> transactions)
    {
        db.AssetStoreTransactions.AddRange(transactions);
    }

    public Task<NestedTransaction> BeginTransaction()
    {
        return db.BeginTransactionSafe();
    }

    public Task<int> SaveChanges()
    {
        return db.SaveChangesAsync();
    }
}