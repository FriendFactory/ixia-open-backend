using System;
using System.Threading.Tasks;
using Frever.Client.Core.Features.InAppPurchases.Subscriptions.Core;
using Frever.Client.Core.Features.InAppPurchases.Subscriptions.Data;
using Microsoft.Extensions.DependencyInjection;

namespace Frever.Client.Core.Features.InAppPurchases.Subscriptions;

public static class ServiceConfiguration
{
    public static void AddInAppSubscriptions(this IServiceCollection services)
    {
        services.AddScoped<ISubscriptionInfoRepository, PersistentSubscriptionInfoRepository>();

        services.AddScoped<IInAppSubscriptionManager, InAppSubscriptionManager>();
    }
}