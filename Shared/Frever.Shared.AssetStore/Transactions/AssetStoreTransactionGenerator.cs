using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Frever.Cache;
using Frever.Shared.AssetStore.DataAccess;
using Frever.Shared.MainDb.Entities;

namespace Frever.Shared.AssetStore.Transactions;

public class AssetStoreTransactionGenerator(
    IAssetStoreTransactionRepository repo,
    IBlobCache<ServiceGroups> groupsCache,
    AssetStoreOptions options
) : IAssetStoreTransactionGenerationService
{
    private static readonly Dictionary<AssetStoreTransactionType, AssetStoreTransactionType> UserTransactionTypeToSystemTypeMappings =
        new()
        {
            {AssetStoreTransactionType.CharacterCreation, AssetStoreTransactionType.SystemIncome},
            {AssetStoreTransactionType.InAppPurchase, AssetStoreTransactionType.SystemIncome},
            {AssetStoreTransactionType.InAppPurchaseRefund, AssetStoreTransactionType.SystemIncome},
            {AssetStoreTransactionType.LevelCreation, AssetStoreTransactionType.SystemIncome},
            {AssetStoreTransactionType.OutfitCreation, AssetStoreTransactionType.SystemIncome},
            {AssetStoreTransactionType.DirectPurchase, AssetStoreTransactionType.SystemIncome},
            {AssetStoreTransactionType.PremiumPurchase, AssetStoreTransactionType.SystemIncome},
            {AssetStoreTransactionType.PurchaseLevel, AssetStoreTransactionType.SystemIncome},
            {AssetStoreTransactionType.Achievement, AssetStoreTransactionType.SystemExpense},
            {AssetStoreTransactionType.HelicopterMoney, AssetStoreTransactionType.SystemExpense},
            {AssetStoreTransactionType.LevelUp, AssetStoreTransactionType.SystemExpense},
            {AssetStoreTransactionType.TaskCompletion, AssetStoreTransactionType.SystemExpense},
            {AssetStoreTransactionType.UserStatusPayout, AssetStoreTransactionType.SystemExpense},
            {AssetStoreTransactionType.DailyQuest, AssetStoreTransactionType.SystemExpense},
            {AssetStoreTransactionType.InitialAccountBalance, AssetStoreTransactionType.SystemExpense},
            {AssetStoreTransactionType.Invitation, AssetStoreTransactionType.SystemExpense},
            {AssetStoreTransactionType.CreatorLevelUp, AssetStoreTransactionType.SystemExpense},
            {AssetStoreTransactionType.OnboardingReward, AssetStoreTransactionType.SystemExpense},
            {AssetStoreTransactionType.CrewReward, AssetStoreTransactionType.SystemExpense},
            {AssetStoreTransactionType.PublishedVideoShare, AssetStoreTransactionType.SystemExpense},
            {AssetStoreTransactionType.VideoRating, AssetStoreTransactionType.SystemExpense},
            {AssetStoreTransactionType.AiWorkflowRun, AssetStoreTransactionType.SystemIncome},
            {AssetStoreTransactionType.AiWorkflowRunErrorRefund, AssetStoreTransactionType.SystemExpense},
            {AssetStoreTransactionType.MonthlyTokenBurnout, AssetStoreTransactionType.SystemIncome},
            {AssetStoreTransactionType.MonthlyTokenRefill, AssetStoreTransactionType.SystemExpense},
            {AssetStoreTransactionType.DailyTokenBurnout, AssetStoreTransactionType.SystemIncome},
            {AssetStoreTransactionType.DailyTokenRefill, AssetStoreTransactionType.SystemExpense},
        };

    private readonly IBlobCache<ServiceGroups> _groupsCache = groupsCache ?? throw new ArgumentNullException(nameof(groupsCache));
    private readonly AssetStoreOptions _options = options ?? throw new ArgumentNullException(nameof(options));
    private readonly IAssetStoreTransactionRepository _repo = repo ?? throw new ArgumentNullException(nameof(repo));

    public async Task<AssetStoreTransaction[]> HardCurrencyPayout(
        long groupId,
        int amount,
        AssetStoreTransactionType transactionType,
        long? userActivityId
    )
    {
        var transaction = new AssetStoreTransaction
                          {
                              CreatedTime = DateTime.UtcNow,
                              GroupId = groupId,
                              HardCurrencyAmount = amount,
                              TransactionType = transactionType,
                              TransactionGroup = Guid.NewGuid(),
                              UserActivityId = userActivityId
                          };

        return await GenerateDoubleBookkeepingTransactions(transaction);
    }

    public Task<AssetStoreTransaction[]> HardCurrencySpend(
        long groupId,
        int amount,
        AssetStoreTransactionType transactionType,
        long? userActivityId
    )
    {
        return HardCurrencyPayout(groupId, amount, transactionType, userActivityId);
    }

    public async Task<AssetStoreTransaction[]> InAppPurchase(
        long groupId,
        long inAppProductId,
        AssetToPurchase[] assets,
        int hardCurrencyAmount,
        int softCurrencyAmount,
        int usdAmountCents,
        string inAppPurchaseRef
    )
    {
        var transactionGroup = Guid.NewGuid();

        var transaction = new AssetStoreTransaction
                          {
                              CreatedTime = DateTime.UtcNow,
                              TransactionGroup = transactionGroup,
                              GroupId = groupId,
                              HardCurrencyAmount = hardCurrencyAmount,
                              SoftCurrencyAmount = softCurrencyAmount,
                              UsdAmountCents = -usdAmountCents,
                              InAppPurchaseRef = inAppPurchaseRef,
                              InAppProductId = inAppProductId,
                              TransactionType = AssetStoreTransactionType.InAppPurchase,
                              AssetStoreTransactionAssets = assets.Select(
                                                                       i => new AssetStoreTransactionAsset
                                                                            {
                                                                                AssetId = i.AssetId, AssetType = i.AssetType
                                                                            }
                                                                   )
                                                                  .ToHashSet()
                          };

        return await GenerateDoubleBookkeepingTransactions(transaction);
    }

    /// <summary>
    ///     Creates three transaction:
    ///     - User transaction: subtract hard and soft currency, add USD
    ///     - System income: add system hard and soft currency
    ///     - System expense: subtract USD
    /// </summary>
    public async Task<AssetStoreTransaction[]> InAppPurchaseRefund(
        long groupId,
        long inAppProductId,
        AssetToPurchase[] assets,
        int hardCurrencyAmount,
        int softCurrencyAmount,
        int usdAmountCents
    )
    {
        var transactionGroup = Guid.NewGuid();

        var transaction = new AssetStoreTransaction
                          {
                              CreatedTime = DateTime.UtcNow,
                              TransactionGroup = transactionGroup,
                              GroupId = groupId,
                              HardCurrencyAmount = -Math.Abs(hardCurrencyAmount),
                              SoftCurrencyAmount = -Math.Abs(softCurrencyAmount),
                              UsdAmountCents = Math.Abs(usdAmountCents),
                              InAppProductId = inAppProductId,
                              TransactionType = AssetStoreTransactionType.InAppPurchaseRefund,
                              AssetStoreTransactionAssets = assets.Select(
                                                                       i => new AssetStoreTransactionAsset
                                                                            {
                                                                                AssetId = i.AssetId, AssetType = i.AssetType
                                                                            }
                                                                   )
                                                                  .ToHashSet()
                          };

        return await GenerateDoubleBookkeepingTransactions(transaction);
    }

    public async Task<AssetStoreTransaction[]> HelicopterMoney(long groupId, int? softCurrencyAmount, int? hardCurrencyAmount)
    {
        var transaction = new AssetStoreTransaction
                          {
                              TransactionGroup = Guid.NewGuid(),
                              GroupId = groupId,
                              TransactionType = AssetStoreTransactionType.HelicopterMoney,
                              SoftCurrencyAmount = softCurrencyAmount.GetValueOrDefault(),
                              HardCurrencyAmount = hardCurrencyAmount.GetValueOrDefault(),
                              CreatedTime = DateTime.UtcNow
                          };

        return await GenerateDoubleBookkeepingTransactions(transaction);
    }

    public async Task<AssetStoreTransaction[]> InitialAccountBalancePayout(long groupId, int softCurrency, int hardCurrency)
    {
        var transaction = new AssetStoreTransaction
                          {
                              CreatedTime = DateTime.UtcNow,
                              GroupId = groupId,
                              SoftCurrencyAmount = softCurrency,
                              HardCurrencyAmount = hardCurrency,
                              TransactionType = AssetStoreTransactionType.InitialAccountBalance,
                              TransactionGroup = Guid.NewGuid()
                          };

        return await GenerateDoubleBookkeepingTransactions(transaction);
    }

    public async Task<AssetStoreTransaction[]> InitialDailyTokens(long groupId, int tokens)
    {
        var transaction = new AssetStoreTransaction
                          {
                              CreatedTime = DateTime.UtcNow,
                              GroupId = groupId,
                              SoftCurrencyAmount = 0,
                              HardCurrencyAmount = tokens,
                              TransactionType = AssetStoreTransactionType.DailyTokenRefill,
                              TransactionGroup = Guid.NewGuid()
                          };

        return await GenerateDoubleBookkeepingTransactions(transaction);
    }

    public Task<AssetStoreTransaction[]> ExchangeHardCurrency(
        long groupId,
        int hardCurrency,
        int softCurrency,
        long hardCurrencyExchangeOfferId
    )
    {
        var transaction = new AssetStoreTransaction
                          {
                              CreatedTime = DateTime.UtcNow,
                              GroupId = groupId,
                              SoftCurrencyAmount = softCurrency,
                              HardCurrencyAmount = -Math.Abs(hardCurrency),
                              TransactionType = AssetStoreTransactionType.HardCurrencyExchange,
                              TransactionGroup = Guid.NewGuid(),
                              HardCurrencyExchangeOfferId = hardCurrencyExchangeOfferId
                          };

        return GenerateDoubleBookkeepingTransactions(transaction);
    }

    public async Task<AssetStoreTransaction[]> AiWorkflowRun(
        long groupId,
        string aiWorkflow,
        long? aiContentId,
        decimal? aiWorkflowUnits,
        int hardCurrencyAmount
    )
    {
        if (hardCurrencyAmount <= 0)
            throw new ArgumentException("Hard currency amount must be positive", nameof(hardCurrencyAmount));

        var transaction = new AssetStoreTransaction
                          {
                              CreatedTime = DateTime.UtcNow,
                              TransactionGroup = Guid.NewGuid(),
                              GroupId = groupId,
                              EntityRefId = aiContentId,
                              TransactionType = AssetStoreTransactionType.AiWorkflowRun,
                              AiWorkflow = aiWorkflow,
                              AiWorkflowBillingUnits = aiWorkflowUnits,
                              HardCurrencyAmount = -hardCurrencyAmount
                          };
        return await GenerateDoubleBookkeepingTransactions(transaction);
    }

    public async Task<AssetStoreTransaction[]> AiWorkflowRefund(
        long aiContentId,
        long groupId,
        Guid transactionGroupId,
        string aiWorkflow,
        decimal? aiWorkflowUnits,
        int hardCurrencyAmount
    )
    {
        if (hardCurrencyAmount <= 0)
            throw new ArgumentException("Hard currency amount must be positive", nameof(hardCurrencyAmount));

        var transaction = new AssetStoreTransaction
                          {
                              TransactionGroup = transactionGroupId,
                              GroupId = groupId,
                              EntityRefId = aiContentId,
                              TransactionType = AssetStoreTransactionType.AiWorkflowRunErrorRefund,
                              AiWorkflow = aiWorkflow,
                              AiWorkflowBillingUnits = aiWorkflowUnits,
                              HardCurrencyAmount = hardCurrencyAmount,
                              CreatedTime = DateTime.UtcNow
                          };
        return await GenerateDoubleBookkeepingTransactions(transaction);
    }

    public async Task<AssetStoreTransaction[]> MonthlyTokenBurnout(
        long groupId,
        Guid transactionGroup,
        int amount,
        long? inAppSubscriptionId
    )
    {
        if (amount >= 0)
            throw new ArgumentException("Burnout amount must be negatvie", nameof(amount));

        var transaction = new AssetStoreTransaction
                          {
                              CreatedTime = DateTime.UtcNow,
                              GroupId = groupId,
                              TransactionGroup = transactionGroup,
                              TransactionType = AssetStoreTransactionType.MonthlyTokenBurnout,
                              HardCurrencyAmount = amount,
                              InAppUserSubscriptionId = inAppSubscriptionId
                          };

        return await GenerateDoubleBookkeepingTransactions(transaction);
    }

    public async Task<AssetStoreTransaction[]> MonthlyTokenRefill(
        long groupId,
        Guid transactionGroup,
        int amount,
        long? inAppSubscriptionId
    )
    {
        if (amount <= 0)
            throw new ArgumentException("Refill amount must be positive", nameof(amount));

        var transaction = new AssetStoreTransaction
                          {
                              CreatedTime = DateTime.UtcNow,
                              GroupId = groupId,
                              TransactionGroup = transactionGroup,
                              TransactionType = AssetStoreTransactionType.MonthlyTokenRefill,
                              HardCurrencyAmount = amount,
                              InAppUserSubscriptionId = inAppSubscriptionId
                          };

        return await GenerateDoubleBookkeepingTransactions(transaction);
    }

    public async Task<AssetStoreTransaction[]> DailyTokenBurnout(long groupId, Guid transactionGroup, int amount, long? inAppSubscriptionId)
    {
        if (amount >= 0)
            throw new ArgumentException("Burnout amount must be negatvie", nameof(amount));

        var transaction = new AssetStoreTransaction
                          {
                              CreatedTime = DateTime.UtcNow,
                              GroupId = groupId,
                              TransactionGroup = transactionGroup,
                              TransactionType = AssetStoreTransactionType.DailyTokenBurnout,
                              HardCurrencyAmount = amount,
                              InAppUserSubscriptionId = inAppSubscriptionId
                          };

        return await GenerateDoubleBookkeepingTransactions(transaction);
    }

    public async Task<AssetStoreTransaction[]> DailyTokenRefill(long groupId, Guid transactionGroup, int amount, long? inAppSubscriptionId)
    {
        if (amount <= 0)
            throw new ArgumentException("Refill amount must be positive", nameof(amount));

        var transaction = new AssetStoreTransaction
                          {
                              CreatedTime = DateTime.UtcNow,
                              GroupId = groupId,
                              TransactionGroup = transactionGroup,
                              TransactionType = AssetStoreTransactionType.DailyTokenRefill,
                              HardCurrencyAmount = amount,
                              InAppUserSubscriptionId = inAppSubscriptionId
                          };

        return await GenerateDoubleBookkeepingTransactions(transaction);
    }

    public async Task<ServiceGroups> GetServiceGroups()
    {
        return await _groupsCache.GetOrCache(
                   $"{CacheKeys.FreverPrefix}::asset-store::service-groups",
                   async () =>
                   {
                       var systemGroup = await _repo.FindGroupByEmail(_options.SystemUserEmail);
                       var supportGroup = await _repo.FindGroupByEmail(_options.CustomerSupportUserEmail);
                       var realMoneyGroup = await _repo.FindGroupByEmail(_options.RealMoneyUserEmail);

                       if (systemGroup == null)
                           throw new InvalidOperationException("System group is not found");
                       if (supportGroup == null)
                           throw new InvalidOperationException("Customer support group is not found");
                       if (realMoneyGroup == null)
                           throw new InvalidOperationException("Real money support group is not found");

                       return new ServiceGroups
                              {
                                  CustomerSupportGroupId = supportGroup.Value,
                                  SystemGroupId = systemGroup.Value,
                                  RealMoneySystemGroupId = realMoneyGroup.Value
                              };
                   },
                   TimeSpan.FromDays(7)
               );
    }

    private async Task<AssetStoreTransaction[]> GenerateDoubleBookkeepingTransactions(params AssetStoreTransaction[] regular)
    {
        if (regular == null || regular.Length == 0)
            throw new ArgumentNullException(nameof(regular));

        var groups = await GetServiceGroups();

        if (regular.FirstOrDefault()?.TransactionType == AssetStoreTransactionType.HardCurrencyExchange)
        {
            var exchange = regular.First();
            var doubleBkTransactions = new[]
                                       {
                                           new AssetStoreTransaction
                                           {
                                               CreatedTime = exchange.CreatedTime,
                                               GroupId = groups.SystemGroupId,
                                               HardCurrencyAmount = 0,
                                               SoftCurrencyAmount = -exchange.SoftCurrencyAmount,
                                               TransactionGroup = exchange.TransactionGroup,
                                               HardCurrencyExchangeOfferId = exchange.HardCurrencyExchangeOfferId,
                                               TransactionType = AssetStoreTransactionType.SystemExpense
                                           },
                                           new AssetStoreTransaction
                                           {
                                               CreatedTime = exchange.CreatedTime,
                                               GroupId = groups.SystemGroupId,
                                               HardCurrencyAmount = -exchange.HardCurrencyAmount,
                                               SoftCurrencyAmount = 0,
                                               TransactionGroup = exchange.TransactionGroup,
                                               HardCurrencyExchangeOfferId = exchange.HardCurrencyExchangeOfferId,
                                               TransactionType = AssetStoreTransactionType.SystemIncome
                                           }
                                       };
            return regular.Concat(doubleBkTransactions).ToArray();
        }

        if (regular.FirstOrDefault()?.TransactionType == AssetStoreTransactionType.InAppPurchase)
        {
            var purchase = regular.First();
            var doubleBkTransactions = new[]
                                       {
                                           new AssetStoreTransaction
                                           {
                                               TransactionType = AssetStoreTransactionType.SystemExpense,
                                               CreatedTime = purchase.CreatedTime,
                                               GroupId = groups.SystemGroupId,
                                               HardCurrencyAmount = -Math.Abs(purchase.HardCurrencyAmount),
                                               SoftCurrencyAmount = -Math.Abs(purchase.SoftCurrencyAmount),
                                               TransactionGroup = purchase.TransactionGroup,
                                               InAppProductId = purchase.InAppProductId,
                                               InAppPurchaseRef = purchase.InAppPurchaseRef
                                           },
                                           new AssetStoreTransaction
                                           {
                                               TransactionType = AssetStoreTransactionType.SystemIncome,
                                               CreatedTime = purchase.CreatedTime,
                                               GroupId = groups.RealMoneySystemGroupId,
                                               HardCurrencyAmount = 0,
                                               SoftCurrencyAmount = 0,
                                               UsdAmountCents = Math.Abs(purchase.UsdAmountCents),
                                               TransactionGroup = purchase.TransactionGroup,
                                               InAppProductId = purchase.InAppProductId,
                                               InAppPurchaseRef = purchase.InAppPurchaseRef
                                           }
                                       };
            return regular.Concat(
                               doubleBkTransactions.Where(
                                   t => t.HardCurrencyAmount != 0 || t.SoftCurrencyAmount != 0 || t.UsdAmountCents != 0
                               )
                           )
                          .ToArray();
        }

        if (regular.FirstOrDefault()?.TransactionType == AssetStoreTransactionType.InAppPurchaseRefund)
        {
            var purchase = regular.First();
            var doubleBkTransactions = new[]
                                       {
                                           new AssetStoreTransaction
                                           {
                                               TransactionType = AssetStoreTransactionType.SystemIncome,
                                               CreatedTime = purchase.CreatedTime,
                                               GroupId = groups.SystemGroupId,
                                               HardCurrencyAmount = Math.Abs(purchase.HardCurrencyAmount),
                                               SoftCurrencyAmount = Math.Abs(purchase.SoftCurrencyAmount),
                                               TransactionGroup = purchase.TransactionGroup,
                                               InAppProductId = purchase.InAppProductId,
                                               InAppPurchaseRef = purchase.InAppPurchaseRef
                                           },
                                           new AssetStoreTransaction
                                           {
                                               TransactionType = AssetStoreTransactionType.SystemExpense,
                                               CreatedTime = purchase.CreatedTime,
                                               GroupId = groups.RealMoneySystemGroupId,
                                               HardCurrencyAmount = 0,
                                               SoftCurrencyAmount = 0,
                                               UsdAmountCents = -Math.Abs(purchase.UsdAmountCents),
                                               TransactionGroup = purchase.TransactionGroup,
                                               InAppProductId = purchase.InAppProductId,
                                               InAppPurchaseRef = purchase.InAppPurchaseRef
                                           }
                                       };
            return regular.Concat(
                               doubleBkTransactions.Where(
                                   t => t.HardCurrencyAmount != 0 || t.SoftCurrencyAmount != 0 || t.UsdAmountCents != 0
                               )
                           )
                          .ToArray();
        }
        else
        {
            var doubleBkTransactions = regular.Where(t => UserTransactionTypeToSystemTypeMappings.ContainsKey(t.TransactionType))
                                              .Select(
                                                   t => new AssetStoreTransaction
                                                        {
                                                            CreatedTime = t.CreatedTime,
                                                            GroupId =
                                                                t.TransactionType == AssetStoreTransactionType.HelicopterMoney
                                                                    ? groups.CustomerSupportGroupId
                                                                    : groups.SystemGroupId,
                                                            HardCurrencyAmount = t.HardCurrencyAmount == 0 ? 0 : -t.HardCurrencyAmount,
                                                            SoftCurrencyAmount = t.SoftCurrencyAmount == 0 ? 0 : -t.SoftCurrencyAmount,
                                                            TransactionGroup = t.TransactionGroup,
                                                            TransactionType = UserTransactionTypeToSystemTypeMappings[t.TransactionType]
                                                        }
                                               )
                                              .ToArray();

            return regular.Concat(doubleBkTransactions).ToArray();
        }
    }
}