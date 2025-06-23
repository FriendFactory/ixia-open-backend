using System;
using Microsoft.Extensions.DependencyInjection;

namespace Frever.Video.Core.Features.Shared;

public static class ServiceConfiguration
{
    public static void AddVideoSharedFeatures(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddScoped<IOneVideoAccessor, PersistentOneVideoAccessor>();
        services.AddScoped<ITaggingGroupProvider, PersistentTaggingGroupProvider>();
    }
}