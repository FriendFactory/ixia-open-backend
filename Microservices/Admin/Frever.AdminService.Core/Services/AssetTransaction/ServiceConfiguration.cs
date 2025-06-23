using Microsoft.Extensions.DependencyInjection;

namespace Frever.AdminService.Core.Services.AssetTransaction;

public static class ServiceConfiguration
{
    public static void AddAssetStoreTransactionAdmin(this IServiceCollection services)
    {
        services.AddScoped<IAssetStoreTransactionService, AssetStoreTransactionService>();
    }
}