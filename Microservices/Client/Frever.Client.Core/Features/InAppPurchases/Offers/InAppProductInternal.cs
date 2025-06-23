using System;
using System.Collections.Generic;
using Frever.Shared.MainDb.Entities;

namespace Frever.Client.Core.Features.InAppPurchases.Offers;

public class InAppProductInternal
{
    public long Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string AppStoreProductRef { get; set; }
    public string PlayMarketProductRef { get; set; }
    public bool IsSeasonPass { get; set; }

    /// <summary>
    ///     The products with the same or close prices are put in the same price group.
    ///     The bigger value of price group the higher price of the product.
    /// </summary>
    public int PriceGroup { get; set; }

    public int SortOrder { get; set; }
    public Dictionary<int, List<InAppProductDetailsInternal>> ProductDetails { get; set; }
    public DateTime? PublicationDate { get; set; }
    public DateTime? DepublicationDate { get; set; }
    public int? GemCount { get; set; }
    public bool IsFreeProduct { get; set; }
    public bool IsSubscription { get; set; }
    public int? MonthlyHardCurrency { get; set; }
    public int DailyHardCurrency { get; set; }
    public bool IsPopular { get; set; }

    public UsageEstimation UsageEstimation { get; set; } = new UsageEstimation();

    public InAppProductInternal ShallowCopy()
    {
        return (InAppProductInternal) MemberwiseClone();
    }
}