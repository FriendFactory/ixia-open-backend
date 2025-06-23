using System;
using System.Threading.Tasks;
using Frever.Common.IntegrationTesting;
using Frever.Shared.MainDb.Entities;

namespace Frever.Client.Core.IntegrationTest.Features.InAppPurchase.Data;

public static class InAppPurchaseData
{
    public static async Task<InAppPurchaseOrder> WithInAppPurchase(
        this DataEnvironment dataEnv,
        long groupId,
        long inAppProductId,
        string receipt
    )
    {
        var inAppPurchase = new InAppPurchaseOrder
                            {
                                Environment = "test",
                                Platform = "iOS",
                                Receipt = receipt,
                                CompletedTime = DateTime.UtcNow,
                                CreatedTime = DateTime.UtcNow,
                                GroupId = groupId,
                                IsPending = false,
                                RefClientCurrency = "USD",
                                StoreOrderIdentifier = "xaxaxxxa",
                                InAppProductId = inAppProductId,
                                RefClientCurrencyPrice = 10,
                                RefPriceUsdCents = 1000,
                                InAppProductOfferKey = "AXXD",
                            };

        dataEnv.Db.InAppPurchaseOrder.Add(inAppPurchase);
        await dataEnv.Db.SaveChangesAsync();

        return inAppPurchase;
    }
}