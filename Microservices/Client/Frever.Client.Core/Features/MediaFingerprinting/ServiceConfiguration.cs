using System;
using Microsoft.Extensions.DependencyInjection;

namespace Frever.Client.Core.Features.MediaFingerprinting;

public static class ServiceConfiguration
{
    public static void AddMediaFingerprinting(this IServiceCollection services, MediaFingerprintingOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.Validate();

        services.AddSingleton(options);
        services.AddScoped<IMediaFingerprintingService, AcrCloudMediaFingerprintingService>();
    }
}