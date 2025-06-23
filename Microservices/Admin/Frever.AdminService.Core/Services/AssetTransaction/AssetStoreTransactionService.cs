using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AuthServer.Permissions.Services;
using Frever.Shared.AssetStore.Transactions;
using Frever.Shared.MainDb;
using Microsoft.EntityFrameworkCore;

namespace Frever.AdminService.Core.Services.AssetTransaction;

public interface IAssetStoreTransactionService
{
    Task<IReadOnlyList<long>> IncreaseUsersCurrencyAsync(IncreaseCurrencySupplyInfo request, CancellationToken token = default);
}

public class AssetStoreTransactionService(
    IWriteDb db,
    IAssetStoreTransactionGenerationService transactionGenerator,
    IUserPermissionService permissionService
) : IAssetStoreTransactionService
{
    public async Task<IReadOnlyList<long>> IncreaseUsersCurrencyAsync(IncreaseCurrencySupplyInfo request, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.GroupIds);

        await permissionService.EnsureHasSocialAccess();

        await using var transaction = await db.BeginTransaction();

        var groupResult = new List<long>();

        var batches = request.GroupIds.Select((gId, idx) => new {Id = gId, Index = idx / 100}).GroupBy(a => a.Index).ToArray();
        foreach (var batch in batches)
        {
            var groupIds = await db.Group.Where(g => batch.Select(e => e.Id).Contains(g.Id))
                                   .Where(g => !g.IsBlocked && g.DeletedAt == null)
                                   .Select(g => g.Id)
                                   .ToArrayAsync(token);

            foreach (var id in groupIds)
            {
                var transactions = await transactionGenerator.HelicopterMoney(id, request.SoftCurrencyAmount, request.HardCurrencyAmount);
                await db.AssetStoreTransactions.AddRangeAsync(transactions, token);

                groupResult.Add(id);
            }
        }

        if (groupResult.Count <= 0)
            return groupResult;

        await db.SaveChangesAsync(token);
        await transaction.CommitAsync(token);

        return groupResult;
    }
}