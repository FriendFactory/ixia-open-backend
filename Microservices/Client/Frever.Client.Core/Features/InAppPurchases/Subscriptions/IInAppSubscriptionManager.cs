using System;
using System.Threading.Tasks;

namespace Frever.Client.Core.Features.InAppPurchases.Subscriptions;

public interface IInAppSubscriptionManager
{
    Task<int> GetDailyTokensAmount(long groupId);

    Task<Balance> ActivateSubscription(Guid inAppOrderId, long inAppProductId);

    /// <summary>
    /// Checks if it's time to renew a subscription token and performs renewing.
    /// Method returns the date of the next renewal. 
    /// </summary>
    Task<Balance> RenewSubscriptionTokens();

    Task CancelAllSubscriptions();
}