using System;
using Frever.Video.Core.Features.Sharing.DataAccess;
using Microsoft.Extensions.DependencyInjection;

namespace Frever.Video.Core.Features.Sharing;

public static class ServiceConfiguration
{
    public static void AddVideoSharing(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IVideoShareRepository, PersistentVideoShareRepository>();
        services.AddScoped<IVideoShareService, VideoShareService>();

        services.AddScoped<IPublicVideoContentService, PublicVideoContentService>();
    }
}