using System;

namespace Frever.Shared.MainDb.Entities;

public class InAppPurchaseOrder
{
    public Guid Id { get; set; }
    public long GroupId { get; set; }
    public bool IsPending { get; set; }
    public DateTime CreatedTime { get; set; }
    public bool? PremiumPassPurchase { get; set; }
    public long InAppProductId { get; set; }
    public string InAppProductOfferKey { get; set; }
    public DateTime? CompletedTime { get; set; }
    public string Platform { get; set; }
    public string Receipt { get; set; }
    public string ErrorCode { get; set; }
    public int RefPriceUsdCents { get; set; }

    /// <summary>
    ///     Gets or sets refernce number of gems for order.
    ///     Value is set only for orders which contains only gems
    ///     and used to determine max gem equivalent.
    /// </summary>
    public int? RefHardCurrencyAmount { get; set; }

    /// <summary>
    ///     Gets or sets OrderID for Play Market or Transaction ID for App Store.
    ///     Used to refund purchase order.
    /// </summary>
    public string StoreOrderIdentifier { get; set; }

    public bool WasRefund { get; set; }
    public long? SeasonId { get; set; }

    /// <summary>
    ///     "sandbox" or "production"
    /// </summary>
    public string Environment { get; set; }

    /// <summary>
    ///     Get or sets the client currency (received from client)
    /// </summary>
    public string RefClientCurrency { get; set; }

    /// <summary>
    ///     Gets or sets a price in client currency (received from client)
    /// </summary>
    public decimal? RefClientCurrencyPrice { get; set; }

    public long? RefInAppProductId { get; set; }
}