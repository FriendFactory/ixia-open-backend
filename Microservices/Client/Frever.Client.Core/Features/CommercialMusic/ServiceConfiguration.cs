using System;
using Common.Infrastructure.MusicProvider;
using FluentValidation;
using Frever.Cache.PubSub;
using Frever.Client.Core.Features.CommercialMusic.BlokurClient;
using Frever.Client.Core.Features.CommercialMusic.BlokurClient.Http;
using Frever.Client.Core.Features.CommercialMusic.LicenseChecking;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Frever.Client.Core.Features.CommercialMusic;

public static class ServiceConfiguration2
{
    public static void AddCommercialMusic(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddMusicProviderOAuth();
        services.AddMusicProviderOAuthSettings(configuration);
        services.AddMusicProviderApiSettings(configuration);

        services.AddScoped<IMusicProviderService, MusicProviderService>();
        services.AddScoped<IValidator<SignUrlRequest>, SignUrlRequestValidator>();
        services.AddScoped<I7DigitalClient, Http7DigitalClient>();
        services.AddScoped<I7DigitalProxyService, Http7DigitalProxyService>();

        var blokurSettings = new HttpBlokurClientSettings();
        configuration.GetSection("Blokur")?.Bind(blokurSettings);
        blokurSettings.Validate();
        services.AddBlokurClient(blokurSettings);

        services.AddRedisPublishing();
        services.AddScoped<IContentDeletionClient, RpcContentDeletionClient>();

        services.AddScoped<IMusicLicenseCheckService, MusicLicenseCheckService>();
        services.AddScoped<IMusicLicenseCheckRepository, PersistentMusicLicenseCheckRepository>();


        services.AddLocalMusicSearch(configuration);
    }
}