using System;
using Frever.Video.Core.Features.Hashtags.DataAccess;
using Frever.Video.Core.Features.Hashtags.Feed;
using Frever.Video.Core.Features.Hashtags.Workers;
using Microsoft.Extensions.DependencyInjection;

namespace Frever.Video.Core.Features.Hashtags;

public static class ServiceConfiguration
{
    public static void AddHashtagStatsUpdating(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IHashtagStatsUpdaterRepository, PersistentHashtagStatsUpdaterRepository>();
        services.AddScoped<IHashtagRepository, PersistentHashtagRepository>();
        services.AddScoped<IHashtagService, HashtagService>();
        services.AddScoped<IHashtagVideoFeed, HashtagVideoFeed>();

        services.AddHostedService<HashtagViewsCountSynchronizerBackgroundWorker>();
    }
}