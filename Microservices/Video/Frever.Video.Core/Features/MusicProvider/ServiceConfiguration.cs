using System;
using Common.Infrastructure.MusicProvider;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Frever.Video.Core.Features.MusicProvider;

public static class ServiceConfiguration
{
    public static void AddMusicProvider(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddMusicProviderOAuth();
        services.AddMusicProviderOAuthSettings(configuration);
        services.AddMusicProviderApiSettings(configuration);

        services.AddScoped<IMusicProviderService, MusicProviderService>();
    }
}