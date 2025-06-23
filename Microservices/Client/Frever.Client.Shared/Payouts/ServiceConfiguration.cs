using System;
using Frever.Shared.AssetStore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Frever.Client.Shared.Payouts;

public static class ServiceConfiguration
{
    public static void AddPayouts(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddScoped<ICurrencyPayoutRepository, PersistentCurrencyPayoutRepository>();
        services.AddScoped<ICurrencyPayoutService, CurrencyPayoutService>();

        services.AddAssetStoreTransactions();
    }
}