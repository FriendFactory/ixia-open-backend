using System;
using Common.Infrastructure.MusicProvider;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Frever.AdminService.Core.Services.MusicProvider;

public static class ServiceConfiguration
{
    public static void AddMusicProvider(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddMusicProviderOAuth();
        services.AddMusicProviderOAuthSettings(configuration);
        services.AddScoped<IMusicProviderService, MusicProviderService>();
        services.AddScoped<IValidator<MusicProviderRequest>, MusicProviderRequestValidator>();
    }
}