using System.Threading.Tasks;
using Frever.Shared.MainDb;
using Microsoft.EntityFrameworkCore;

namespace Frever.Client.Core.Features.InAppPurchases;

public class AssetStoreTransactionBalanceService(IWriteDb db) : IBalanceService
{
    /// <summary>
    /// Gets balance of tokens with details of each token type.
    /// There are three type of tokens:
    /// - Permanent, bought via in-app purchase
    /// - Subscription (monthly), granted on subscription refreshing
    /// - Daily tokens, granted daily.
    ///
    /// To get total user balance, we sum up all transactions of user.
    /// To get monthly or daily token amount, system looks for latest monthly/daily refill.
    /// System always burns out remained tokens and refill the full amount. 
    /// </summary>
    public async Task<BalanceInfo> GetBalance(long groupId)
    {
        var balance = await db.GetGroupBalanceInfo([groupId]).FirstOrDefaultAsync();
        return balance ?? new BalanceInfo {DailyTokens = 0, PermanentTokens = 0, SubscriptionTokens = 0};
    }
}