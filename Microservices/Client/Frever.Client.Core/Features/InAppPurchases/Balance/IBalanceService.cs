using System;
using System.Threading.Tasks;
using Frever.Protobuf;
using Frever.Shared.MainDb;

namespace Frever.Client.Core.Features.InAppPurchases;

public interface IBalanceService
{
    Task<BalanceInfo> GetBalance(long groupId);
}

public class Balance
{
    public required long GroupId { get; set; }
    public required int TotalTokens { get; set; }
    public required int DailyTokens { get; set; }
    public required int MaxDailyTokens { get; set; }
    public required int SubscriptionTokens { get; set; }
    public required int? MaxSubscriptionTokens { get; set; }
    public required int PermanentTokens { get; set; }
    public required DateTime NextDailyTokenRefresh { get; set; }
    public required DateTime? NextSubscriptionTokenRefresh { get; set; }

    public required string ActiveSubscription { get; set; }
}