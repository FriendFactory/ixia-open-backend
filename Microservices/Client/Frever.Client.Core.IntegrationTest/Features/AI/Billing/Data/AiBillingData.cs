using Frever.Common.IntegrationTesting;
using Frever.Shared.MainDb.Entities;
using Microsoft.EntityFrameworkCore;

namespace Frever.Client.Core.IntegrationTest.Features.AI.Billing.Data;

public static class AiBillingData
{
    public static async Task<AiWorkflowMetadata[]> WithAiWorkflowPrices(this DataEnvironment data)
    {
        ArgumentNullException.ThrowIfNull(data);

        for (var i = 0; i < 10; i++)
        {
            var workflowPrice = new AiWorkflowMetadata
                                {
                                    AiWorkflow = $"workflow_{Guid.NewGuid():N}",
                                    IsActive = i % 2 == 0,
                                    HardCurrencyPrice = 10 + Random.Shared.Next(1, 20),
                                    RequireBillingUnits = i % 3 == 0
                                };
            data.Db.AiWorkflowMetadata.Add(workflowPrice);
        }

        await data.Db.SaveChangesAsync();

        return await data.Db.AiWorkflowMetadata.ToArrayAsync();
    }

    public static async Task WithInitialBalance(this DataEnvironment data, long groupId, int availabeHardCurrency)
    {
        ArgumentNullException.ThrowIfNull(data);

        data.Db.AssetStoreTransactions.Add(
            new AssetStoreTransaction
            {
                GroupId = groupId,
                TransactionGroup = Guid.NewGuid(),
                TransactionType = AssetStoreTransactionType.InitialAccountBalance,
                HardCurrencyAmount = availabeHardCurrency,
                HardCurrencyAmountNoDiscount = availabeHardCurrency,
                CreatedTime = DateTime.UtcNow
            }
        );

        await data.Db.SaveChangesAsync();
    }
}