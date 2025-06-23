using System;
using Frever.Video.Contract;
using Frever.Video.Core.Features.VideoInfoExtraData;
using Frever.Video.Core.Features.VideoInformation.DataAccess;
using Microsoft.Extensions.DependencyInjection;

namespace Frever.Video.Core.Features.VideoInformation;

public static class ServiceConfiguration
{
    public static void AddVideoInfoLoading(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IVideoExtraDataRepository, PersistentVideoExtraDataRepository>();
        services.AddScoped<IFollowRelationService, PersistentFollowRelationService>();
        services.AddScoped<IVideoExtraDataProvider, VideoExtraDataProvider>();

        services.AddScoped<IVideoLoader, CachedVideoInfoLoader>();
        services.AddScoped<IVideoInfoRepository, PersistentVideoInfoRepository>();
        services.AddScoped<IVideoExtraDataProvider, VideoExtraDataProvider>();
    }
}