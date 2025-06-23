using System;
using System.Linq;
using System.Threading.Tasks;
using Common.Infrastructure;
using Common.Models;
using Frever.Shared.AssetStore.Transactions;
using Frever.Shared.MainDb.Entities;
using Microsoft.Extensions.Logging;

namespace Frever.Client.Shared.Payouts;

public class CurrencyPayoutService : ICurrencyPayoutService
{
    private readonly ILogger _log;
    private readonly ICurrencyPayoutRepository _repo;
    private readonly IAssetStoreTransactionGenerationService _transactionGenerator;

    public CurrencyPayoutService(
        ILoggerFactory loggerFactory,
        ICurrencyPayoutRepository repo,
        IAssetStoreTransactionGenerationService transactionGenerator
    )
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);

        _repo = repo ?? throw new ArgumentNullException(nameof(repo));
        _transactionGenerator = transactionGenerator ?? throw new ArgumentNullException(nameof(transactionGenerator));
        _log = loggerFactory.CreateLogger("Frever.UserActivity.SoftCurrencyPayout");
    }

    public async Task<long> HardCurrencyPayout(long groupId, int amount, AssetStoreTransactionType transactionType, long? userActivityId)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be positive");

        var transactions = await _transactionGenerator.HardCurrencyPayout(groupId, amount, transactionType, userActivityId);

        await _repo.RecordAssetStoreTransaction(transactions);

        _log.LogInformation(
            "Hard currency paid: {Amount} with type {Type} [transaction IDs={Id}]",
            amount,
            transactionType,
            string.Join(",", transactions.Select(t => t.Id.ToString()))
        );

        return transactions.First(t => t.GroupId == groupId).Id;
    }

    public async Task<long> SpendHardCurrency(long groupId, int amount, AssetStoreTransactionType transactionType, long? userActivityId)
    {
        if (amount > 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be negative or zero");

        var balance = await _repo.GetHardCurrencyBalance(groupId);

        if (balance < Math.Abs(amount))
            throw AppErrorWithStatusCodeException.BadRequest("Not enough currency", ErrorCodes.Client.PurchaseNotEnoughCurrency);

        var transactions = await _transactionGenerator.HardCurrencySpend(groupId, amount, transactionType, userActivityId);

        await _repo.RecordAssetStoreTransaction(transactions);

        _log.LogInformation(
            "Hard currency spend: {Amount} with type {Type} [transaction IDs={Id}]",
            amount,
            transactionType,
            string.Join(",", transactions.Select(t => t.Id.ToString()))
        );

        return transactions.First(t => t.GroupId == groupId).Id;
    }

    public async Task ExchangeHardCurrencyToSoft(long groupId, int hardCurrency, int softCurrency, long hardCurrencyExchangeOfferId)
    {
        if (hardCurrency <= 0)
            throw new ArgumentOutOfRangeException(nameof(hardCurrency), "Hard currency amount must be positive");
        if (softCurrency <= 0)
            throw new ArgumentOutOfRangeException(nameof(softCurrency), "Soft currency amount must be positive");

        var hcBalance = await _repo.GetHardCurrencyBalance(groupId);
        if (hcBalance < hardCurrency)
            throw AppErrorWithStatusCodeException.BadRequest("Not enough currency", ErrorCodes.Client.PurchaseNotEnoughCurrency);

        var transactions = await _transactionGenerator.ExchangeHardCurrency(
                               groupId,
                               hardCurrency,
                               softCurrency,
                               hardCurrencyExchangeOfferId
                           );

        await _repo.RecordAssetStoreTransaction(transactions);

        _log.LogInformation(
            "Group {GroupId} Exchange hard currency amount {HC} to soft currency amount {SC} using offer {ExchangeOfferId}",
            groupId,
            hardCurrency,
            softCurrency,
            hardCurrencyExchangeOfferId
        );
    }

    public async Task AddInitialAccountBalance(long groupId, int hardCurrency, int softCurrency)
    {
        if (hardCurrency <= 0 && softCurrency <= 0)
            throw new ArgumentOutOfRangeException(nameof(hardCurrency), "Hard or soft currency amount must be positive");

        var initialized = await _repo.GroupHasTransaction(groupId, AssetStoreTransactionType.InitialAccountBalance);
        if (initialized)
        {
            _log.LogInformation("Initial account balance has already been added");
            return;
        }

        var transactions = await _transactionGenerator.InitialAccountBalancePayout(groupId, hardCurrency, softCurrency);

        await _repo.RecordAssetStoreTransaction(transactions);

        _log.LogInformation(
            "Add initial account balance for group {GroupId}: {SC} soft and {HC} hard currency",
            groupId,
            softCurrency,
            hardCurrency
        );
    }

    public async Task AddInitialDailyTokens(long groupId, int dailyTokens)
    {
        if (dailyTokens < 0)
            throw new ArgumentOutOfRangeException(nameof(dailyTokens), "Daily token amount must be positive");

        var refilled = await _repo.GroupHasTransaction(groupId, AssetStoreTransactionType.DailyTokenRefill);
        if (refilled)
        {
            _log.LogInformation("Initial account balance has already been added");
            return;
        }

        var transactions = await _transactionGenerator.InitialDailyTokens(groupId, dailyTokens);

        await _repo.RecordAssetStoreTransaction(transactions);

        _log.LogInformation("Add initial daily tokens group {GroupId}: {dailyTokens} daily tokens", groupId, dailyTokens);
    }
}