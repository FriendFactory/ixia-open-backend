using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Frever.Client.Shared.AI.PixVerse;

public static class ServiceConfiguration
{
    public static void AddPixVerseProxy(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var aiSettings = new PixVerseSettings();
        configuration.Bind("AI", aiSettings);
        aiSettings.Validate();

        services.AddSingleton(aiSettings);
        services.AddScoped<IPixVerseProxy, PixVerseProxy>();
    }
}