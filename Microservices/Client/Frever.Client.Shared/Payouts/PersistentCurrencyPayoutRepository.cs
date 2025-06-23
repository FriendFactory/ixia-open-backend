using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Frever.Shared.MainDb;
using Frever.Shared.MainDb.Entities;
using Microsoft.EntityFrameworkCore;

namespace Frever.Client.Shared.Payouts;

public class PersistentCurrencyPayoutRepository(IWriteDb mainDb) : ICurrencyPayoutRepository
{
    private readonly IWriteDb _mainDb = mainDb ?? throw new ArgumentNullException(nameof(mainDb));

    public async Task RecordAssetStoreTransaction(IEnumerable<AssetStoreTransaction> transactions)
    {
        ArgumentNullException.ThrowIfNull(transactions);

        foreach (var t in transactions)
            await _mainDb.AssetStoreTransactions.AddAsync(t);

        await _mainDb.SaveChangesAsync();
    }

    public Task<int> GetHardCurrencyBalance(long groupId)
    {
        return _mainDb.AssetStoreTransactions.Where(t => t.GroupId == groupId)
                      .Where(t => t.HardCurrencyAmount != 0)
                      .SumAsync(t => t.HardCurrencyAmount);
    }

    public Task<bool> GroupHasTransaction(long groupId, AssetStoreTransactionType type)
    {
        return _mainDb.AssetStoreTransactions.AnyAsync(t => t.GroupId == groupId && t.TransactionType == type);
    }
}