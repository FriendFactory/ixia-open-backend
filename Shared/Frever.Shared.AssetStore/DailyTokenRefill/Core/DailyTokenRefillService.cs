using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Frever.Shared.AssetStore.DailyTokenRefill.DataAccess;
using Frever.Shared.AssetStore.Transactions;
using Frever.Shared.MainDb;
using Frever.Shared.MainDb.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Frever.Shared.AssetStore.DailyTokenRefill.Core;

public class DailyTokenRefillService(
    IAssetStoreTransactionGenerationService generator,
    IDailyTokenRefillRepository repo,
    ILoggerFactory loggerFactory
) : IDailyTokenRefillService
{
    private const int BatchSize = 100;
    private readonly ILogger log = loggerFactory.CreateLogger("Ixia.Economy.DailyTokenRefill");

    public async Task RefillDailyTokens(long groupId)
    {
        using var scope = log.BeginScope("RefillDailyTokens groupId={groupId}: ", groupId);

        var balance = await repo.GetBalance([groupId]).FirstOrDefaultAsync();
        if (balance == null)
        {
            log.LogError("Can't get balance");
            throw new InvalidOperationException($"Can't get balance for group {groupId}");
        }

        var activeSubscription = await repo.GetGroupActiveSubscription(false).Where(s => s.GroupId == groupId).FirstOrDefaultAsync();
        if (activeSubscription == null)
        {
            log.LogError("Can't get active subscription info");
            throw new InvalidOperationException($"Can't get subscription info for group {groupId}");
        }

        using var scope2 = log.BeginScope(
            "Current Daily Tokens Balance={dailyTokenBalance}; Daily Tokens To Grant={dailyTokensToGrant}; Active Subscription={activeSubscriptionId} :",
            balance.DailyTokens,
            activeSubscription.DailyTokens,
            activeSubscription.SubscriptionId
        );

        if (activeSubscription.DailyTokens == balance.DailyTokens)
        {
            log.LogInformation("Daily token amount is actual, no refill needed");
            return;
        }

        var (burnout, refill) = await GenerateTransactions(
                                    groupId,
                                    balance.DailyTokens,
                                    activeSubscription.DailyTokens,
                                    activeSubscription.SubscriptionId
                                );

        repo.AddTransactions(burnout);
        await repo.SaveChanges();

        repo.AddTransactions(refill);
        await repo.SaveChanges();

        log.LogInformation(
            "Daily token refilled. Transactions generated: {transactionInfoJson}",
            JsonConvert.SerializeObject(
                burnout.Concat(refill)
               .Select(
                    t => new
                         {
                             Id = t.Id,
                             Group = t.TransactionGroup,
                             Type = t.TransactionType,
                             Amount = t.HardCurrencyAmount,
                             CreatedTime = t.CreatedTime
                         }
                )
            )
        );
    }

    public async Task BatchRefillDailyTokens(bool forceRefill = true)
    {
        var totalGroups = 0;
        var sw = Stopwatch.StartNew();

        await ProcessUserBatches(
            async batch =>
            {
                await RefillDailyTokens(batch, forceRefill);
                totalGroups += batch.Length;
            },
            forceRefill
        );

        sw.Stop();

        log.LogInformation("Daily token refilled for {numOfGroups} groups in {time}", totalGroups, sw.Elapsed);
    }

    private async Task RefillDailyTokens(GroupActiveSubscriptionInfo[] batch, bool forceRefill)
    {
        await using var transaction = await repo.BeginTransaction();

        var ids = batch.Select(s => s.GroupId).ToArray();

        var balance = await repo.GetBalance(ids).ToArrayAsync();
        var data = batch.Join(balance, s => s.GroupId, b => b.GroupId, (s, b) => new {Subscription = s, Balance = b}).ToArray();

        var allBurnout = new List<AssetStoreTransaction>();
        var allRefill = new List<AssetStoreTransaction>();

        foreach (var item in data.Where(a => a.Balance.DailyTokens != a.Subscription.DailyTokens))
        {
            var (burnout, refill) = await GenerateTransactions(
                                        item.Subscription.GroupId,
                                        item.Balance.DailyTokens,
                                        item.Subscription.DailyTokens,
                                        item.Subscription.SubscriptionId
                                    );

            allBurnout.AddRange(burnout);
            allRefill.AddRange(refill);

            log.LogInformation(
                "GroupID={groupId}: Daily token refilled from {balance} to {targetTokens}. Subscription ID={subscriptionId}",
                item.Subscription.GroupId,
                item.Balance.DailyTokens,
                item.Subscription.DailyTokens,
                item.Subscription.SubscriptionId
            );
        }

        repo.AddTransactions(allBurnout);
        await repo.SaveChanges();

        repo.AddTransactions(allRefill);
        await repo.SaveChanges();


        await transaction.Commit();
    }

    private async Task<(AssetStoreTransaction[] burnout, AssetStoreTransaction[] refill)> GenerateTransactions(
        long groupId,
        int currentDailyTokens,
        int targetDailyTokens,
        long? subscriptionId
    )
    {
        var transactionGroupId = Guid.NewGuid();
        var burnout = currentDailyTokens > 0
                          ? await generator.DailyTokenBurnout(groupId, transactionGroupId, -currentDailyTokens, subscriptionId)
                          : [];

        var refill = await generator.DailyTokenRefill(groupId, transactionGroupId, targetDailyTokens, subscriptionId);

        return (burnout, refill);
    }


    private async Task ProcessUserBatches(Func<GroupActiveSubscriptionInfo[], Task> action, bool forceRefill)
    {
        var batch = await repo.GetGroupActiveSubscription(!forceRefill).OrderByDescending(s => s.GroupId).Take(BatchSize).ToArrayAsync();

        if (!batch.Any())
            return;

        int batchProcessAttempt = 4;

        do
        {
            for (var i = 0; i < batchProcessAttempt; i++)
            {
                try
                {
                    await action(batch);
                    continue;
                }
                catch (Exception ex)
                {
                    log.LogError(ex, "Error processing batch");
                    if (i == batchProcessAttempt - 1)
                        throw;
                }
            }

            var minGroupId = batch.Min(s => s.GroupId);
            batch = await repo.GetGroupActiveSubscription(!forceRefill)
                              .Where(s => s.GroupId < minGroupId)
                              .OrderByDescending(s => s.GroupId)
                              .Take(BatchSize)
                              .ToArrayAsync();
        }
        while (batch.Any());
    }
}