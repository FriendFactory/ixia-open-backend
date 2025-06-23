using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Common.Infrastructure.ModerationProvider;

public static class ModerationProviderApiConfiguration
{
    public static void AddModerationProviderApi(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddScoped<IModerationProviderApi, ModerationProviderApi>();
        services.AddOptions<ModerationProviderApiSettings>().Bind(configuration.GetSection(nameof(ModerationProviderApiSettings)));
    }
}