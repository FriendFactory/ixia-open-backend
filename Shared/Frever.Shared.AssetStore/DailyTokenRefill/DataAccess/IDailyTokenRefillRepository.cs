using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Infrastructure.Database;
using Frever.Shared.MainDb;
using Frever.Shared.MainDb.Entities;

namespace Frever.Shared.AssetStore.DailyTokenRefill.DataAccess;

public interface IDailyTokenRefillRepository
{
    IQueryable<BalanceInfo> GetBalance(long[] groupIds);
    IQueryable<GroupActiveSubscriptionInfo> GetGroupActiveSubscription(bool excludeGroupsWithRefilledDailyTokens);

    void AddTransactions(IEnumerable<AssetStoreTransaction> transactions);

    Task<NestedTransaction> BeginTransaction();
    Task<int> SaveChanges();
}