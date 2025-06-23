using Frever.Common.IntegrationTesting;
using Frever.Shared.MainDb.Entities;
using Microsoft.EntityFrameworkCore;

namespace Frever.Client.Core.IntegrationTest.Features.InAppPurchase.Data;

public static class InAppSubscriptionData
{
    public static async Task<InAppUserSubscription> WithInAppUserSubscription(
        this DataEnvironment dataEnv,
        long groupId,
        DateTime startDate,
        DateTime? endDate,
        int dailyHardCurrency,
        int monthlyHardCurrency,
        long refInAppProductId,
        string receipt = null
    )
    {
        var inAppProduct = await dataEnv.Db.InAppProduct.FirstOrDefaultAsync(p => p.Id == refInAppProductId);

        var inAppPurchase = new InAppPurchaseOrder
                            {
                                Environment = "test",
                                Platform = "ios",
                                Receipt = receipt ?? "test_receipt",
                                CompletedTime = DateTime.UtcNow,
                                CreatedTime = DateTime.UtcNow,
                                GroupId = groupId,
                                IsPending = false,
                                RefClientCurrency = "usd",
                                StoreOrderIdentifier = "xixixixixixx",
                                InAppProductId = inAppProduct.Id,
                                InAppProductOfferKey = "abbc"
                            };
        dataEnv.Db.InAppPurchaseOrder.Add(inAppPurchase);
        await dataEnv.Db.SaveChangesAsync();


        var s = new InAppUserSubscription
                {
                    Status = "Active",
                    StartedAt = startDate.ToUniversalTime(),
                    CompletedAt = endDate?.ToUniversalTime(),
                    GroupId = groupId,
                    DailyHardCurrency = dailyHardCurrency,
                    MonthlyHardCurrency = monthlyHardCurrency,
                    RefInAppProductId = refInAppProductId,
                    InAppPurchaseOrderId = inAppPurchase.Id
                };

        dataEnv.Db.InAppUserSubscription.Add(s);
        await dataEnv.Db.SaveChangesAsync();
        return s;
    }
}