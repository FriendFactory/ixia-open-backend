using System;
using Frever.Protobuf;

namespace Frever.ClientService.Contract.Social;

public class UserBalance
{
    public required int HardCurrencyAmount { get; set; }
    public required int DailyTokens { get; set; }
    public required int SubscriptionTokens { get; set; }
    public required int PermanentTokens { get; set; }
    [ProtoNewField(1)] public required DateTime NextDailyTokenRefresh { get; set; }
    [ProtoNewField(2)] public required DateTime? NextSubscriptionTokenRefresh { get; set; }
    [ProtoNewField(3)] public required int MaxDailyTokens { get; set; }
    [ProtoNewField(4)] public required int? MaxSubscriptionTokens { get; set; }
    [ProtoNewField(5)] public string Subscription { get; set; }
}