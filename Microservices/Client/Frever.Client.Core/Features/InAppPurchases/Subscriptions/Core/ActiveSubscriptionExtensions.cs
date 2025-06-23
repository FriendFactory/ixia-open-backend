using System;
using System.Linq;
using System.Threading.Tasks;
using Frever.Shared.MainDb.Entities;

namespace Frever.Client.Core.Features.InAppPurchases.Subscriptions.Core;

public static class ActiveSubscriptionExtensions
{
    public static IQueryable<InAppUserSubscription> ActiveSubscriptions(this IQueryable<InAppUserSubscription> source, DateTime? at = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        var atDate = at ?? DateTime.UtcNow.Date;

        return source.Where(s => s.StartedAt.Date <= atDate && (s.CompletedAt == null || s.CompletedAt.Value.Date >= atDate));
    }
}