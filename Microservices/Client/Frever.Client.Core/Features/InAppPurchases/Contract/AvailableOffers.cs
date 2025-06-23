using System;
using Frever.Protobuf;

namespace Frever.Client.Core.Features.InAppPurchases.Contract;

public class AvailableOffers
{
    public InAppProductOffer[] HardCurrencyOffers { get; set; } = [];

    [Obsolete] public InAppProductSlot[] InAppProducts { get; set; } = [];

    [ProtoNewField(1)] public InAppProductOffer[] SubscriptionOffers { get; set; } = [];
}