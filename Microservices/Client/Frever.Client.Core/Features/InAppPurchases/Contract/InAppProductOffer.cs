using System.Collections.Generic;
using Frever.Protobuf;
using Frever.Shared.MainDb.Entities;

namespace Frever.Client.Core.Features.InAppPurchases.Contract;

public class InAppProductOffer
{
    public long Id { get; set; }
    public string OfferKey { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string AppStoreProductRef { get; set; }
    public string PlayMarketProductRef { get; set; }
    public List<InAppProductOfferDetails> Details { get; set; } = [];

    [ProtoNewField(1)] public bool IsSubscription { get; set; }
    [ProtoNewField(2)] public int? MonthlyHardCurrency { get; set; }
    [ProtoNewField(3)] public int DailyHardCurrency { get; set; }

    [ProtoNewField(4)] public UsageEstimation UsageEstimation { get; set; }
    [ProtoNewField(5)] public bool IsPopular { get; set; }
}