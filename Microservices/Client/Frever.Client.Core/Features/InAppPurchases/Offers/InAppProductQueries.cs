using System;
using System.Collections.Generic;
using System.Linq;

namespace Frever.Client.Core.Features.InAppPurchases.Offers;

public static class InAppProductQueries
{
    public static IEnumerable<InAppProductInternal> HardCurrencyOnly(this IEnumerable<InAppProductInternal> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return source.Where(
            p => p.ProductDetails.Any() && p.ProductDetails.All(
                     kvp => kvp.Value.All(d => d.HardCurrency != null && d.SoftCurrency == null && d.AssetId == null)
                 )
        );
    }


    public static IEnumerable<InAppProductInternal> SubscriptionsOnly(this IEnumerable<InAppProductInternal> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return source.Where(p => p.IsSubscription);
    }

    public static IEnumerable<InAppProductInternal> ExcludeHardCurrencyOnly(this IEnumerable<InAppProductInternal> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var hc = source.HardCurrencyOnly().ToArray();

        return source.Where(p => !hc.Contains(p));
    }
}