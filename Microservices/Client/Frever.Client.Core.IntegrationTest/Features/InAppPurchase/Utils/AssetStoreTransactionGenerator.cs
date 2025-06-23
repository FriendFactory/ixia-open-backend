using Frever.Client.Core.IntegrationTest.Features.InAppPurchase.Data;
using Frever.Common.IntegrationTesting;
using Frever.Shared.MainDb.Entities;

namespace Frever.Client.Core.IntegrationTest.Features.InAppPurchase;

public class AssetStoreTransactionGenerator(DataEnvironment dataEnv, long groupId)
{
    public async Task PurchaseTokens(int amount, DateTime date)
    {
        if (amount <= 0)
            throw new ArgumentException("Amount must be positive", nameof(amount));

        await dataEnv.WithAssetStoreTransaction(groupId, AssetStoreTransactionType.InAppPurchase, amount, date);
    }

    public async Task RefillDaily(int amount, DateTime date)
    {
        if (amount <= 0)
            throw new ArgumentException("Amount must be positive", nameof(amount));

        await dataEnv.WithAssetStoreTransaction(groupId, AssetStoreTransactionType.DailyTokenRefill, amount, date);
    }

    public async Task BurnoutDaily(int amount, DateTime date)
    {
        if (amount > 0)
            throw new ArgumentException("Amount must be negative", nameof(amount));

        await dataEnv.WithAssetStoreTransaction(groupId, AssetStoreTransactionType.DailyTokenBurnout, amount, date);
    }

    public async Task RefillMonthly(int amount, DateTime date)
    {
        if (amount <= 0)
            throw new ArgumentException("Amount must be positive", nameof(amount));

        await dataEnv.WithAssetStoreTransaction(groupId, AssetStoreTransactionType.MonthlyTokenRefill, amount, date);
    }

    public async Task BurnoutMonthly(int amount, DateTime date)
    {
        if (amount > 0)
            throw new ArgumentException("Amount must be negative", nameof(amount));

        await dataEnv.WithAssetStoreTransaction(groupId, AssetStoreTransactionType.MonthlyTokenBurnout, amount, date);
    }

    public async Task RunWorkflow(int amount, DateTime date)
    {
        if (amount > 0)
            throw new ArgumentException("Amount must be negative", nameof(amount));

        await dataEnv.WithAssetStoreTransaction(groupId, AssetStoreTransactionType.AiWorkflowRun, amount, date);
    }
}