using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthServerShared;
using Frever.Client.Core.Features.AppStoreApi;
using Frever.Client.Core.Features.InAppPurchases.Contract;
using Frever.Client.Core.Features.InAppPurchases.DataAccess;
using Frever.ClientService.Contract.InAppPurchases;
using Frever.Shared.MainDb.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Platform = Frever.ClientService.Contract.Common.Platform;

namespace Frever.Client.Core.Features.InAppPurchases.InAppPurchase;

public interface IInAppPurchaseRestoreService
{
    Task<RestoreInAppPurchaseResult> RestoreInAppPurchases(RestoreInAppPurchaseRequest request);
}

public class InAppPurchaseRestoreService(
    UserInfo currentUser,
    IAppStoreApiClient appStoreClient,
    IInAppProductOfferService offerService,
    IInAppPurchaseService purchaseService,
    IPurchaseOrderRepository purchaseOrderRepo,
    IInAppProductRepository inAppProductRepository,
    ILoggerFactory loggerFactory
) : IInAppPurchaseRestoreService
{
    private readonly ILogger log = loggerFactory.CreateLogger("Ixia.Economy.InAppPurchase.Restore");

    public async Task<RestoreInAppPurchaseResult> RestoreInAppPurchases(RestoreInAppPurchaseRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        switch (request.Platform)
        {
            case Platform.iOS:
                return await RestoreAppStorePurchases(request.TransactionId);
            case Platform.Android:
                throw new NotImplementedException();
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private async Task<RestoreInAppPurchaseResult> RestoreAppStorePurchases(string transactionId)
    {
        using var scope = log.BeginScope(
            "Restore AppStore In-App Purchases Group ID={groupId} App Store Transaction ID={appStoreTransactionId} : ",
            currentUser.UserMainGroupId,
            transactionId
        );

        var existingOrders = await purchaseOrderRepo.GetUserOrders(currentUser)
                                                    .Where(o => o.Platform == Platform.iOS.ToString("G"))
                                                    .ToArrayAsync();

        log.LogInformation(
            "Found {numOrders} existing orders. Order IDs={orderIds}",
            existingOrders.Length,
            String.Join(", ", existingOrders.Select(o => o.Id.ToString()))
        );

        // Try to find transaction for current user or use provided transaction ID if not found
        var existingTransactionId = existingOrders.Where(o => !String.IsNullOrWhiteSpace(o.StoreOrderIdentifier))
                                                  .Select(o => o.StoreOrderIdentifier)
                                                  .FirstOrDefault() ?? transactionId;

        if (String.IsNullOrWhiteSpace(existingTransactionId))
            throw new ArgumentNullException(nameof(transactionId));

        var offers = await offerService.GetOffers();

        var appStorePurchaseHistory = await appStoreClient.TransactionHistory(existingTransactionId);

        log.LogInformation(
            "Found {numAppStoreOrders} AppStore orders. Transaction IDs={appStoreTransactionIds}",
            appStorePurchaseHistory.Length,
            String.Join(", ", appStorePurchaseHistory.Select(t => t.TransactionId))
        );

        var consumables = await RestoreConsumables(offers, appStorePurchaseHistory, existingOrders);
        var subscriptions = await RestoreSubscriptions(existingTransactionId, offers);

        var details = consumables.Concat(subscriptions).ToArray();

        return new RestoreInAppPurchaseResult
               {
                   Ok = !details.Any() || details.All(d => d.WasRestored),
                   Details = details,
                   SubscriptionRestored = subscriptions.Select(s => s.InAppProductTitle).FirstOrDefault(),
                   PermanentTokenRestored = details.Any() ? details.Sum(d => d.HardCurrency) : 0
               };
    }

    private async Task<InAppPurchaseRestoreDetails[]> RestoreConsumables(
        AvailableOffers offers,
        AppStoreTransactionStatus[] appStoreTransactions,
        InAppPurchaseOrder[] existingOrders
    )
    {
        var existingPurchaseOrderIds = existingOrders.Select(o => o.StoreOrderIdentifier).ToHashSet();

        var notExisting = appStoreTransactions.Where(t => !t.IsSubscription)
                                              .Where(t => !existingPurchaseOrderIds.Contains(t.TransactionId))
                                              .ToArray();

        log.LogInformation("AppStore transaction to restore: {transactionData}", JsonConvert.SerializeObject(notExisting));

        var result = new List<InAppPurchaseRestoreDetails>();

        foreach (var t in notExisting)
        {
            var appStoreProductRef = t.InAppProductId;
            var offer = offers.HardCurrencyOffers.FirstOrDefault(t => t.AppStoreProductRef == appStoreProductRef);

            if (offer == null)
            {
                result.Add(
                    new InAppPurchaseRestoreDetails
                    {
                        ErrorMessage = "Offer for product is not available",
                        HardCurrency = 0,
                        IsSubscription = false,
                        TransactionId = t.TransactionId,
                        WasRestored = false,
                        InAppProductId = null,
                        StoreProductRef = t.InAppProductId,
                        InAppProductTitle = String.Empty,
                        IsFromAnotherAccount = false,
                        InAppPurchaseOrderId = null
                    }
                );

                log.LogError(
                    "Error restoring transaction ID={transactionId}: offer for the product {appStoreProductRef} is not available",
                    t.TransactionId,
                    t.InAppProductId
                );

                continue;
            }

            var pending = await purchaseService.InitInAppPurchase(
                              new InitInAppPurchaseRequest
                              {
                                  ClientCurrency = t.Currency, ClientCurrencyPrice = t.Price, InAppProductOfferKey = offer.OfferKey
                              }
                          );

            if (!pending.Ok)
            {
                result.Add(
                    new InAppPurchaseRestoreDetails
                    {
                        ErrorMessage = "Error creating pending order: " + pending.ErrorMessage,
                        HardCurrency = offer.Details.Sum(d => d.HardCurrency ?? 0),
                        IsSubscription = false,
                        TransactionId = t.TransactionId,
                        WasRestored = false,
                        InAppProductId = offer.Id,
                        StoreProductRef = t.InAppProductId,
                        InAppProductTitle = offer.Title,
                        IsFromAnotherAccount = false,
                        InAppPurchaseOrderId = null
                    }
                );

                log.LogError(
                    "Error restoring transaction ID={transactionId}: error creating pending " +
                    "order for {appStoreProductRef} offer {offer} in-app product ID={inAppProductId}: error {err}",
                    t.TransactionId,
                    t.InAppProductId,
                    offer.OfferKey,
                    offer.Id,
                    pending.ErrorMessage
                );

                continue;
            }

            await purchaseService.CompleteInAppPurchase(
                new CompleteInAppPurchaseRequest
                {
                    Platform = Platform.iOS, TransactionData = t.TransactionId, PendingOrderId = pending.PendingOrderId
                }
            );
        }

        return result.ToArray();
    }

    private async Task<InAppPurchaseRestoreDetails[]> RestoreSubscriptions(string transactionId, AvailableOffers offers)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(transactionId);
        var subscriptions = await appStoreClient.SubscriptionHistory(transactionId);

        var activeSubscriptionTransaction = subscriptions.LastTransactions.FirstOrDefault(s => s.RenewalInfo != null);

        if (activeSubscriptionTransaction == null)
        {
            log.LogInformation("No active subscription provided from Apple, cancel all active subscriptions");
            await purchaseService.CancelAllSubscriptions();
            return
            [
                new InAppPurchaseRestoreDetails
                {
                    ErrorMessage = null,
                    HardCurrency = 0,
                    IsSubscription = true,
                    TransactionId = null,
                    WasRestored = false,
                    StoreProductRef = null,
                    InAppProductId = null,
                    InAppProductTitle = null,
                    IsFromAnotherAccount = false,
                    InAppPurchaseOrderId = null
                }
            ];
        }
        else
        {
            var offer = offers.SubscriptionOffers.FirstOrDefault(
                s => s.AppStoreProductRef == activeSubscriptionTransaction.RenewalInfo.ProductId
            );
            var renewal = activeSubscriptionTransaction.RenewalInfo;

            if (offer == null)
            {
                log.LogError("No offer for subscription {subscriptionProductId}", renewal.ProductId);
                return
                [
                    new InAppPurchaseRestoreDetails
                    {
                        ErrorMessage = $"No offer for subscription {renewal.ProductId}",
                        HardCurrency = 0,
                        IsSubscription = true,
                        TransactionId = renewal.OriginalTransactionId,
                        WasRestored = false,
                        StoreProductRef = renewal.ProductId,
                        InAppProductId = null,
                        InAppProductTitle = null,
                        IsFromAnotherAccount = false,
                        InAppPurchaseOrderId = null
                    }
                ];
            }

            var order = await purchaseService.InitInAppPurchase(
                            new InitInAppPurchaseRequest
                            {
                                ClientCurrency = activeSubscriptionTransaction.TransactionInfo.Currency,
                                ClientCurrencyPrice = activeSubscriptionTransaction.TransactionInfo.Price / 1000.0M,
                                InAppProductOfferKey = offer.OfferKey
                            }
                        );
            if (!order.Ok)
            {
                return
                [
                    new InAppPurchaseRestoreDetails
                    {
                        ErrorMessage = $"Cannot place order for subscription {renewal.ProductId}: " + order.ErrorMessage,
                        HardCurrency = 0,
                        IsSubscription = true,
                        TransactionId = renewal.OriginalTransactionId,
                        WasRestored = false,
                        StoreProductRef = renewal.ProductId,
                        InAppProductId = null,
                        InAppProductTitle = null,
                        IsFromAnotherAccount = false,
                        InAppPurchaseOrderId = order.PendingOrderId
                    }
                ];
            }

            var complete = await purchaseService.CompleteInAppPurchase(
                               new CompleteInAppPurchaseRequest
                               {
                                   Platform = Platform.iOS,
                                   TransactionData = renewal.OriginalTransactionId,
                                   PendingOrderId = order.PendingOrderId
                               }
                           );

            if (!complete.Ok)
            {
                return
                [
                    new InAppPurchaseRestoreDetails
                    {
                        ErrorMessage = $"Cannot complete order for subscription {renewal.ProductId}: " + complete.ErrorMessage,
                        HardCurrency = 0,
                        IsSubscription = true,
                        TransactionId = renewal.OriginalTransactionId,
                        WasRestored = false,
                        StoreProductRef = renewal.ProductId,
                        InAppProductId = null,
                        InAppProductTitle = null,
                        IsFromAnotherAccount = false,
                        InAppPurchaseOrderId = order.PendingOrderId
                    }
                ];
            }

            return
            [
                new InAppPurchaseRestoreDetails
                {
                    ErrorMessage = null,
                    HardCurrency = 0,
                    IsSubscription = true,
                    TransactionId = renewal.OriginalTransactionId,
                    WasRestored = true,
                    StoreProductRef = renewal.ProductId,
                    InAppProductId = offer.Id,
                    InAppProductTitle = offer.Title,
                    IsFromAnotherAccount = false,
                    InAppPurchaseOrderId = order.PendingOrderId
                }
            ];
        }
    }
}