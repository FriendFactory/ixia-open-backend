using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Frever.Client.Shared.AI.ComfyUi;

public static class ComfyUiApiConfiguration
{
    public static void AddComfyUiApi(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var settings = new ComfyUiApiSettings();
        configuration.Bind("ComfyUiApiSettings", settings);
        settings.Validate();
        services.AddSingleton(settings);

        services.AddScoped<IComfyUiClient, ComfyUiClient>();
    }
}