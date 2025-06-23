using System;
using Frever.Client.Core.Features.InAppPurchases;
using Microsoft.Extensions.DependencyInjection;

namespace Frever.ClientService.Api.Features.InAppPurchases;

public static class ServiceConfiguration
{
    public static void AddInAppPurchases(this IServiceCollection services, InAppPurchaseOptions options)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);

        services.AddInAppPurchasesCore(options);
        services.AddHostedService<AndroidRefundPurchaseWatcher>();
    }
}