using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Common.Infrastructure.MusicProvider;

public static class OAuthSignatureConfiguration
{
    public static void AddMusicProviderOAuth(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IOAuthSignatureProvider, OAuthSignatureProvider>();
    }

    public static void AddMusicProviderOAuthSettings(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<MusicProviderOAuthSettings>().Bind(configuration.GetSection(nameof(MusicProviderOAuthSettings)));
    }

    public static void AddMusicProviderApiSettings(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<MusicProviderApiSettings>().Bind(configuration.GetSection(nameof(MusicProviderApiSettings)));
    }
}