using System;
using System.Threading.Tasks;
using Frever.Client.Core.Features.AppStoreApi.Core;
using Microsoft.Extensions.DependencyInjection;

namespace Frever.Client.Core.Features.AppStoreApi;

public static class ServiceConfiguration
{
    public static void AddAppStoreApi(this IServiceCollection services, AppStoreApiOptions options)
    {
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        options.Validate();

        services.AddSingleton(options);
        services.AddSingleton<IAppStoreApiClient, HttpAppStoreApiClient>();
    }
}