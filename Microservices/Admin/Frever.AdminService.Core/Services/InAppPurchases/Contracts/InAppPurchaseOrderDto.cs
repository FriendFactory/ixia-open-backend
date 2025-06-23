using System;
using Frever.Shared.AssetStore.OfferKeyCodec;

namespace Frever.AdminService.Core.Services.InAppPurchases.Contracts;

public class InAppPurchaseOrderDto
{
    public Guid Id { get; set; }

    public long GroupId { get; set; }

    public bool IsPending { get; set; }

    public DateTime CreatedTime { get; set; }

    public bool? PremiumPassPurchase { get; set; }

    public DateTime? CompletedTime { get; set; }

    public string Platform { get; set; }

    public string ErrorCode { get; set; }

    public int RefPriceUsdCents { get; set; }

    public int? RefHardCurrencyAmount { get; set; }

    public string StoreOrderIdentifier { get; set; }

    public bool WasRefund { get; set; }

    public long? SeasonId { get; set; }

    public InAppProductOfferPayload InAppProductOfferPayload { get; set; }
}