using System;
using System.Threading.Tasks;
using Frever.Shared.MainDb.Entities;

namespace Frever.Shared.AssetStore.Transactions;

public interface IAssetStoreTransactionGenerationService
{
    Task<AssetStoreTransaction[]> InAppPurchase(
        long groupId,
        long inAppProductId,
        AssetToPurchase[] assets,
        int hardCurrencyAmount,
        int softCurrencyAmount,
        int usdAmountCents,
        string inAppPurchaseRef
    );

    Task<AssetStoreTransaction[]> HelicopterMoney(long groupId, int? softCurrencyAmount, int? hardCurrencyAmount);

    Task<AssetStoreTransaction[]> HardCurrencyPayout(
        long groupId,
        int amount,
        AssetStoreTransactionType transactionType,
        long? userActivityId
    );

    Task<AssetStoreTransaction[]> HardCurrencySpend(
        long groupId,
        int amount,
        AssetStoreTransactionType transactionType,
        long? userActivityId
    );

    Task<AssetStoreTransaction[]> InitialAccountBalancePayout(long groupId, int softCurrency, int hardCurrency);

    Task<AssetStoreTransaction[]> InitialDailyTokens(long groupId, int tokens);

    Task<AssetStoreTransaction[]> ExchangeHardCurrency(long groupId, int hardCurrency, int softCurrency, long hardCurrencyExchangeOfferId);

    Task<AssetStoreTransaction[]> InAppPurchaseRefund(
        long groupId,
        long inAppProductId,
        AssetToPurchase[] assets,
        int hardCurrencyAmount,
        int softCurrencyAmount,
        int usdAmountCents
    );

    Task<AssetStoreTransaction[]> AiWorkflowRun(
        long groupId,
        string aiWorkflow,
        long? aiContentId,
        decimal? aiWorkflowUnits,
        int hardCurrencyAmount
    );

    Task<AssetStoreTransaction[]> AiWorkflowRefund(
        long aiContentId,
        long groupId,
        Guid transactionGroupId,
        string aiWorkflow,
        decimal? aiWorkflowUnits,
        int hardCurrencyAmount
    );

    Task<AssetStoreTransaction[]> MonthlyTokenBurnout(long groupId, Guid transactionGroup, int amount, long? inAppSubscriptionId);

    Task<AssetStoreTransaction[]> MonthlyTokenRefill(long groupId, Guid transactionGroup, int amount, long? inAppSubscriptionId);

    Task<AssetStoreTransaction[]> DailyTokenBurnout(long groupId, Guid transactionGroup, int amount, long? inAppSubscriptionId);

    Task<AssetStoreTransaction[]> DailyTokenRefill(long groupId, Guid transactionGroup, int amount, long? inAppSubscriptionId);

    Task<ServiceGroups> GetServiceGroups();
}