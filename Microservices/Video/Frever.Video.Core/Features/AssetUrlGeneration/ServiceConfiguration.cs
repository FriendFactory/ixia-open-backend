using System;
using Microsoft.Extensions.DependencyInjection;

namespace Frever.Video.Core.Features.AssetUrlGeneration;

public static class ServiceConfiguration
{
    public static void AddVideoAssetUrlGeneration(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddScoped<IVideoAssetUrlGenerator, CloudFrontVideoAssetUrlGenerator>();
    }
}