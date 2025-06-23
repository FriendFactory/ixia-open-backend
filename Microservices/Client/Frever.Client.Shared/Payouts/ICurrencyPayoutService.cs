using System.Threading.Tasks;
using Frever.Shared.MainDb.Entities;

namespace Frever.Client.Shared.Payouts;

public interface ICurrencyPayoutService
{
    Task<long> HardCurrencyPayout(long groupId, int amount, AssetStoreTransactionType transactionType, long? userActivityId);

    Task<long> SpendHardCurrency(long groupId, int amount, AssetStoreTransactionType transactionType, long? userActivityId);

    Task ExchangeHardCurrencyToSoft(long groupId, int hardCurrency, int softCurrency, long hardCurrencyExchangeOfferId);

    Task AddInitialAccountBalance(long groupId, int hardCurrency, int softCurrency);
    Task AddInitialDailyTokens(long groupId, int dailyTokens);
}