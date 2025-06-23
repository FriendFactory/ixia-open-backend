using Frever.Cache.Configuration;
using Frever.Shared.MainDb.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace Frever.Videos.Shared.CachedVideoKpis;

public static class ServiceConfiguration
{
    public static void AddCachedVideoKpis(this IServiceCollection services)
    {
        services.AddScoped<IVideoKpiRepository, VideoKpiRepository>();
        services.AddScoped<IVideoKpiCachingService, VideoKpiCachingService>();
        services.AddFreverCaching(o => { o.Redis.Hash<VideoKpi>(); });
    }
}