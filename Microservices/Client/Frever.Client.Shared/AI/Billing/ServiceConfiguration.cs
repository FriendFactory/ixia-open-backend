using System;
using Frever.Client.Shared.AI.Metadata;
using Frever.Shared.AssetStore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Frever.Client.Shared.AI.Billing;

public static class ServiceConfiguration
{
    public static void AddAiBilling(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddAssetStoreTransactions();

        services.AddScoped<IAiBillingRepository, PersistentAiBillingRepository>();
        services.AddScoped<IAiBillingService, AiBillingService>();

        services.AddAiWorkflowMetadata();
    }
}