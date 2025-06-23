using System;
using Frever.Cache.Configuration;
using Frever.Shared.AssetStore.DailyTokenRefill;
using Frever.Shared.AssetStore.DailyTokenRefill.Core;
using Frever.Shared.AssetStore.DailyTokenRefill.DataAccess;
using Frever.Shared.AssetStore.DataAccess;
using Frever.Shared.AssetStore.OfferKeyCodec;
using Frever.Shared.AssetStore.Transactions;
using Microsoft.Extensions.DependencyInjection;

namespace Frever.Shared.AssetStore;

public static class ServiceConfiguration
{
    public static void AddAssetStoreTransactions(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton(new AssetStoreOptions());
        services.AddScoped<IAssetStoreTransactionRepository, PersistentAssetStoreTransactionRepository>();
        services.AddScoped<IAssetStoreTransactionGenerationService, AssetStoreTransactionGenerator>();
        services.AddScoped<IInAppProductOfferKeyCodec, SimpleInAppProductOfferKeyCodec>();
        services.AddFreverCaching(options => { options.InMemory.Blob<ServiceGroups>(); });

        services.AddScoped<IDailyTokenRefillRepository, PersistentDailyTokenRefillRepository>();
        services.AddScoped<IDailyTokenRefillService, DailyTokenRefillService>();
    }
}