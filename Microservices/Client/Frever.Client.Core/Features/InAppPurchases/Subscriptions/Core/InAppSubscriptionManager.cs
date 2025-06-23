using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthServerShared;
using Common.Infrastructure;
using Frever.Client.Core.Features.InAppPurchases.InAppPurchase;
using Frever.Client.Core.Features.InAppPurchases.Subscriptions.Data;
using Frever.Shared.AssetStore.Transactions;
using Frever.Shared.MainDb;
using Frever.Shared.MainDb.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Platform = Frever.ClientService.Contract.Common.Platform;

namespace Frever.Client.Core.Features.InAppPurchases.Subscriptions.Core;

public class InAppSubscriptionManager(
    ISubscriptionInfoRepository repo,
    IBalanceService balanceService,
    ILoggerFactory loggerFactory,
    IAssetStoreTransactionGenerationService transactionGenerator,
    IStoreTransactionDataValidator storeTransactionDataValidator,
    UserInfo currentUser
) : IInAppSubscriptionManager
{
    private const int SubscriptionRefillIntervalDays = 30;

    private readonly ILogger log = loggerFactory.CreateLogger("Ixia.InAppPurchase.Subscriptions");

    public async Task<int> GetDailyTokensAmount(long groupId)
    {
        // Get last active subscription
        var activeSubscription = await repo.GetGroupSubscription(groupId)
                                           .ActiveSubscriptions()
                                           .OrderByDescending(s => s.Id)
                                           .FirstOrDefaultAsync();

        if (activeSubscription != null)
            return activeSubscription.DailyHardCurrency;

        var defaultSubscriptionProduct = await repo.GetInAppProducts()
                                                   .Where(s => !s.IsActive) // !!!! PRODUCT SHOULD BE INACTIVE TO DON'T BE IN OFFER LIST 
                                                   .Where(s => s.IsSubscription && s.IsFreeProduct)
                                                   .OrderByDescending(s => s.Id)
                                                   .FirstOrDefaultAsync();

        if (defaultSubscriptionProduct != null)
            return defaultSubscriptionProduct.DailyHardCurrency;

        return 30;
    }

    public async Task<Balance> ActivateSubscription(Guid inAppOrderId, long inAppProductId)
    {
        using var scope = log.BeginScope(
            "Activate subscription [{rid}]: Group ID={groupId} Product ID={inAppProductId} Order ID={inAppOrderId}",
            Guid.NewGuid(),
            currentUser.UserMainGroupId,
            inAppProductId,
            inAppOrderId
        );

        var inAppProduct = await repo.GetInAppProducts().FirstOrDefaultAsync(p => p.Id == inAppProductId);
        if (inAppProduct == null)
            throw AppErrorWithStatusCodeException.BadRequest("Invalid in-app product", "IN_APP_PRODUCT_NOT_EXIST");
        if (!inAppProduct.IsActive || inAppProduct.IsFreeProduct || inAppProduct.IsSeasonPass ||
            inAppProduct.DepublicationDate < DateTime.UtcNow || inAppProduct.PublicationDate > DateTime.UtcNow ||
            !inAppProduct.IsSubscription)
            throw AppErrorWithStatusCodeException.BadRequest(
                "In-app product is not subscription or not available",
                "IN_APP_PRODUCT_INVALID"
            );

        var purchaseOrder = await repo.GetInAppPurchaseOrder(inAppOrderId).FirstOrDefaultAsync();
        if (purchaseOrder == null)
            throw AppErrorWithStatusCodeException.BadRequest("Purchase order does not exist", "PURCHASE_ORDER_NOT_EXIST");
        if (purchaseOrder.GroupId != currentUser || purchaseOrder.IsPending || purchaseOrder.WasRefund)
            throw AppErrorWithStatusCodeException.BadRequest("Invalid purchase order", "PURCHASE_ORDER_INVALID");

        log.LogInformation("Input looks valid, activate new subscription");

        var activeSubscriptions = await repo.GetGroupSubscription(currentUser)
                                            .Where(s => s.CompletedAt == null || s.CompletedAt > DateTime.UtcNow)
                                            .ToArrayAsync();

        var lastActiveSubscription = activeSubscriptions.OrderByDescending(s => s.Id).FirstOrDefault();

        if (lastActiveSubscription != null && lastActiveSubscription.RefInAppProductId == inAppProductId)
        {
            log.LogInformation("Subscription for Product ID={inAppProductId} already active", inAppProductId);
            return await RenewSubscriptionTokens();
        }

        // Only one active subscription allowed
        foreach (var active in activeSubscriptions)
        {
            var product = await repo.GetInAppProducts().SingleOrDefaultAsync(p => p.Id == active.RefInAppProductId);

            active.CompletedAt = DateTime.UtcNow.Date.AddSeconds(-1);
            active.Status = inAppProduct.MonthlyHardCurrency > product.MonthlyHardCurrency
                                ? InAppUserSubscription.KnownStatusUpgraded
                                : InAppUserSubscription.KnownStatusDowngraded;

            log.LogInformation("Active subscription ID={activeSubscriptionId} is canceled", active.Id);
        }

        await repo.SaveChanges();

        var newSubscription = new InAppUserSubscription
                              {
                                  Status = InAppUserSubscription.KnownStatusActive,
                                  StartedAt = lastActiveSubscription?.StartedAt.ToUniversalTime() ?? DateTime.UtcNow.Date,
                                  GroupId = currentUser,
                                  DailyHardCurrency = inAppProduct.DailyHardCurrency,
                                  MonthlyHardCurrency = inAppProduct.MonthlyHardCurrency,
                                  InAppPurchaseOrderId = purchaseOrder.Id,
                                  RefInAppProductId = inAppProductId,
                                  CreatedAt = DateTime.UtcNow
                              };
        repo.AddInAppUserSubscription(newSubscription);

        await repo.SaveChanges();

        log.LogInformation(
            "New subscription ID={newSubscriptionId} created. Daily Tokens={dailyTokens}; Monthly Tokens={MonthlyTokens}",
            newSubscription.Id,
            newSubscription.DailyHardCurrency,
            newSubscription.MonthlyHardCurrency
        );

        // Refill tokens only on new subscription or upgrading existing subscription
        if (lastActiveSubscription == null || lastActiveSubscription.MonthlyHardCurrency < inAppProduct.MonthlyHardCurrency)
        {
            var transactions = await Refill(newSubscription, Guid.NewGuid());
            await repo.RecordAssetStoreTransactions(transactions);
        }

        return await RenewSubscriptionTokens();
    }

    public async Task<Balance> RenewSubscriptionTokens()
    {
        return await RenewSubscriptionCore(DateTime.UtcNow, true);
    }

    public async Task CancelAllSubscriptions()
    {
        log.LogInformation("Cancel all active subscriptions for group ID={currentGroupId}", currentUser.UserMainGroupId);

        var activeSubscriptions = await repo.GetGroupSubscription(currentUser).ActiveSubscriptions().ToArrayAsync();

        foreach (var s in activeSubscriptions)
        {
            s.CompletedAt = DateTime.UtcNow.AddSeconds(-1);
            s.Status = InAppUserSubscription.KnownStatusCanceled;
            log.LogInformation("Active subscription ID={activeSubscriptionId} is canceled", s.Id);
        }

        await repo.SaveChanges();

        await RenewSubscriptionTokens();
    }

    private async Task<Balance> RenewSubscriptionCore(DateTime now, bool performBurnout)
    {
        now = now.ToUniversalTime();

        using var scope = log.BeginScope(
            "Renew subscription tokens: [{rid}] Group ID={groupId} At={atDate}: ",
            Guid.NewGuid().ToString("N"),
            currentUser.UserMainGroupId,
            now
        );

        var activeSubscription = await repo.GetGroupSubscription(currentUser)
                                           .ActiveSubscriptions(now)
                                           .OrderByDescending(s => s.Id)
                                           .FirstOrDefaultAsync();

        var balance = await balanceService.GetBalance(currentUser);

        var lastRefill = await repo.GetGroupTransactions(currentUser)
                                   .Where(t => t.TransactionType == AssetStoreTransactionType.MonthlyTokenRefill)
                                   .OrderByDescending(t => t.Id)
                                   .FirstOrDefaultAsync();

        var subscriptionProduct = default(InAppProduct);

        if (activeSubscription != null)
            subscriptionProduct = await repo.GetInAppProducts().FirstOrDefaultAsync(p => p.Id == activeSubscription.RefInAppProductId);


        var (needRefill, needBurnout, nextRefillDate) = await IsRefillNeeded(now, balance, activeSubscription, lastRefill);
        var maxDailyTokens = activeSubscription?.DailyHardCurrency;
        var maxMonthlyTokens = activeSubscription?.MonthlyHardCurrency;

        var transactionGroup = Guid.NewGuid();
        var assetStoreTransactions = new List<AssetStoreTransaction>();

        if (performBurnout && needBurnout)
            assetStoreTransactions.AddRange(await Burnout(balance, transactionGroup, activeSubscription?.Id));
        else
            log.LogInformation("No burnout needed");

        if (needRefill)
        {
            var (isValid, expirationDate) = await ValidateSubscription(activeSubscription?.Id);

            if (isValid)
            {
                assetStoreTransactions.AddRange(await Refill(activeSubscription, transactionGroup));
            }
            else
            {
                nextRefillDate = null;
                maxMonthlyTokens = null;
                maxDailyTokens = null;
                log.LogWarning("Subscription is inactive and were deactivated");
            }
        }
        else
            log.LogInformation("No refill needed");


        log.LogDebug("Subscription transactions generated: {transactions}", JsonConvert.SerializeObject(assetStoreTransactions));
        if (assetStoreTransactions.Any())
            await repo.RecordAssetStoreTransactions(assetStoreTransactions);

        var newBalanceInfo = await balanceService.GetBalance(currentUser);


        if (maxDailyTokens == null)
        {
            var freeSubscription = await repo.GetInAppProducts().Where(p => p.IsSubscription && p.IsFreeProduct).FirstOrDefaultAsync();
            maxDailyTokens = freeSubscription?.DailyHardCurrency ?? 30;
        }

        var newBalance = new Balance
                         {
                             NextSubscriptionTokenRefresh = nextRefillDate,
                             GroupId = currentUser,
                             TotalTokens = newBalanceInfo.DailyTokens + newBalanceInfo.PermanentTokens + newBalanceInfo.SubscriptionTokens,
                             DailyTokens = newBalanceInfo.DailyTokens,
                             MaxDailyTokens = maxDailyTokens ?? 30,
                             SubscriptionTokens = newBalanceInfo.SubscriptionTokens,
                             MaxSubscriptionTokens = maxMonthlyTokens,
                             PermanentTokens = newBalanceInfo.PermanentTokens,
                             NextDailyTokenRefresh = DateTime.UtcNow.Date.AddDays(1).AddHours(1), // UTC 01:00:00
                             ActiveSubscription = subscriptionProduct?.Title
                         };

        return newBalance;
    }

    private async Task<(bool needRefill, bool needBurnout, DateTime? nextRefillDate)> IsRefillNeeded(
        DateTime now,
        BalanceInfo balance,
        InAppUserSubscription lastSubscription,
        AssetStoreTransaction lastRefill
    )
    {
        var nowDate = now.Date;

        // No active subscription and balance > 0 -> burnout, no refill, no next refill date
        if (lastSubscription == null || (lastSubscription.CompletedAt != null && lastSubscription?.CompletedAt?.Date < nowDate))
        {
            log.LogInformation("No active subscription, balance={balance}", balance.SubscriptionTokens);
            return (false, balance.SubscriptionTokens > 0, null);
        }

        var currentPeriod = SubscriptionPeriod(lastSubscription, now);
        var lastRefillPeriod = lastRefill == null ? -1 : SubscriptionPeriod(lastSubscription, lastRefill.CreatedTime);

        var nextRefillDate = lastSubscription.StartedAt.Date.AddDays((currentPeriod + 1) * SubscriptionRefillIntervalDays).AddDays(1);

        var needRefill = currentPeriod > lastRefillPeriod;

        var completeDate = lastSubscription.CompletedAt?.Date;

        // No burnout if refill is not planned
        return (needRefill, needRefill && balance.SubscriptionTokens > 0,
                completeDate != null && completeDate < nextRefillDate ? null : nextRefillDate);
    }

    private async Task<AssetStoreTransaction[]> Burnout(BalanceInfo balance, Guid transactionGroup, long? activeSubscriptionId)
    {
        if (balance.SubscriptionTokens > 0)
        {
            log.LogInformation(
                "Current subscription token balance={balance}, perform burnout. Transaction group={transactionGroup}",
                balance.SubscriptionTokens,
                transactionGroup
            );
            var burnoutTransactions = await transactionGenerator.MonthlyTokenBurnout(
                                          currentUser,
                                          transactionGroup,
                                          -balance.SubscriptionTokens,
                                          activeSubscriptionId
                                      );
            return burnoutTransactions;
        }
        else
        {
            log.LogInformation("Burnout is not required, balance={balance}", balance.SubscriptionTokens);
            return [];
        }
    }

    private async Task<AssetStoreTransaction[]> Refill(InAppUserSubscription activeSubscription, Guid transactionGroup)
    {
        log.LogInformation(
            "Refill subscription ID={subscriptionId} for amount={amount}, transaction group={transactionGroup}",
            activeSubscription.Id,
            activeSubscription.MonthlyHardCurrency,
            transactionGroup
        );

        var transactions = await transactionGenerator.MonthlyTokenRefill(
                               currentUser,
                               transactionGroup,
                               activeSubscription.MonthlyHardCurrency,
                               activeSubscription.Id
                           );

        return transactions;
    }

    private async Task<(bool isValid, DateTime? expirationDate)> ValidateSubscription(long? activeSubscriptionId)
    {
        if (activeSubscriptionId == null)
            return (false, null);

        var activeSubscription = await repo.GetGroupSubscription(currentUser).FirstOrDefaultAsync(s => s.Id == activeSubscriptionId);
        if (activeSubscription == null)
        {
            log.LogWarning("Subscription ID={activeSubscriptionId} not found", activeSubscriptionId);
            return (false, null);
        }

        var order = await repo.GetInAppPurchaseOrder(activeSubscription.InAppPurchaseOrderId).FirstOrDefaultAsync();
        if (order == null)
        {
            log.LogWarning("InApp Purchase Order ID={inAppPurchaseOrderId} not found", activeSubscription.InAppPurchaseOrderId);
            return (false, null);
        }

        var receiptValidationResult = await storeTransactionDataValidator.ValidateSubscription(
                                          StringComparer.OrdinalIgnoreCase.Equals("iOS", order.Platform) ? Platform.iOS : Platform.Android,
                                          order.StoreOrderIdentifier
                                      );

        if (!receiptValidationResult.IsActive)
        {
            activeSubscription.Status = InAppUserSubscription.KnownStatusComplete;
            activeSubscription.CompletedAt = DateTime.UtcNow.Date.AddMilliseconds(-1);
            await repo.SaveChanges();

            return (false, activeSubscription.CompletedAt);
        }
        else
        {
            var period = SubscriptionPeriod(activeSubscription, DateTime.UtcNow);
            activeSubscription.Status = InAppUserSubscription.KnownStatusActive;
            activeSubscription.CompletedAt =
                activeSubscription.StartedAt.Date.AddDays((period + 1) * SubscriptionRefillIntervalDays).AddDays(1);

            await repo.SaveChanges();

            return (true, activeSubscription.CompletedAt);
        }
    }

    private static int SubscriptionPeriod(InAppUserSubscription subscription, DateTime date)
    {
        if (subscription == null)
            throw new ArgumentNullException(nameof(subscription));

        return Convert.ToInt32((date.Date - subscription.StartedAt.Date).TotalDays) / SubscriptionRefillIntervalDays;
    }
}