using System;
using Frever.Client.Core.Features.CommercialMusic.BlokurClient.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Frever.Client.Core.Features.CommercialMusic.BlokurClient;

public static class ServiceConfiguration
{
    public static void AddBlokurClient(this IServiceCollection services, HttpBlokurClientSettings settings)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(settings);

        settings.Validate();

        services.AddScoped<IBlokurClient, HttpBlokurClient>();
        services.AddSingleton(settings);
    }
}