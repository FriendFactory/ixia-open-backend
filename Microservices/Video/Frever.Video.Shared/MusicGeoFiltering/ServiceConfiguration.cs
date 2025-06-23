using System;
using Frever.Videos.Shared.MusicGeoFiltering.AbstractApi;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Frever.Videos.Shared.MusicGeoFiltering;

public static class ServiceConfiguration
{
    public static void AddMusicLicenseFiltering(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var abstractApiConfiguration = new AbstractApiConfiguration();
        configuration.Bind("AbstractApi", abstractApiConfiguration);
        abstractApiConfiguration.Validate();
        services.AddSingleton(abstractApiConfiguration);

        services.AddScoped<IMusicGeoFilter, FastMusicGeoFilter>();
        services.AddScoped<ISongChecker, DbBasedSongChecker>();
        services.AddScoped<ICurrentLocationProvider, AbstractApiCurrentLocationProvider>();
        services.AddScoped<IIpAddressProvider, HttpContextIpAddressProvider>();

        services.AddScoped<CountryCodeLookup>();
        services.AddHttpContextAccessor();
        services.AddHttpClient();
    }
}