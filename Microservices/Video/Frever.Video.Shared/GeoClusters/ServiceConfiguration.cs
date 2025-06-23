using Frever.Cache.Configuration;
using Frever.Shared.MainDb.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace Frever.Videos.Shared.GeoClusters;

public static class ServiceConfiguration
{
    public static void AddGeoCluster(this IServiceCollection services)
    {
        services.AddScoped<IGeoClusterProvider, GeoClusterProvider>();
        services.AddFreverCaching(options => { options.InMemoryDoubleCache.Blob<GeoCluster[]>(null, false, typeof(GeoCluster)); });
    }
}