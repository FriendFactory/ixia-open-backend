using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Common.Infrastructure.Database;
using Frever.Shared.MainDb;
using Frever.Shared.MainDb.Entities;
using Microsoft.EntityFrameworkCore;

namespace Frever.Client.Shared.AI.Billing;

public interface IAiBillingRepository
{
    Task<int> GetUserBalance(long groupId);

    Task<AssetStoreTransaction[]> GetTransactionsInGroup(long aiContentId);

    Task SaveBillingTransactions(IEnumerable<AssetStoreTransaction> transactions);

    Task<NestedTransaction> BeginTransaction();
}

public class PersistentAiBillingRepository(IWriteDb db) : IAiBillingRepository
{
    public Task<AssetStoreTransaction[]> GetTransactionsInGroup(long aiContentId)
    {
        var transactionGroup = db.AssetStoreTransactions.Where(e => e.EntityRefId == aiContentId).Select(e => e.TransactionGroup);
        return db.AssetStoreTransactions.Where(e => transactionGroup.Contains(e.TransactionGroup)).ToArrayAsync();
    }

    public Task<int> GetUserBalance(long groupId)
    {
        return db.AssetStoreTransactions.Where(t => t.GroupId == groupId).SumAsync(t => t.HardCurrencyAmount);
    }

    public async Task SaveBillingTransactions(IEnumerable<AssetStoreTransaction> transactions)
    {
        ArgumentNullException.ThrowIfNull(transactions);
        foreach (var t in transactions)
            db.AssetStoreTransactions.Add(t);

        await db.SaveChangesAsync();
    }

    public Task<NestedTransaction> BeginTransaction()
    {
        return db.BeginTransactionSafe(IsolationLevel.RepeatableRead);
    }
}