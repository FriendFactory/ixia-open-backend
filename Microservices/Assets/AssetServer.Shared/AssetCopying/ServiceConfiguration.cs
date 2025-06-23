using System;
using Amazon.SQS;
using AssetStoragePathProviding;
using Microsoft.Extensions.DependencyInjection;

namespace AssetServer.Shared.AssetCopying;

public static class ServiceConfiguration
{
    public static void AddAssetCopying(this IServiceCollection services, AssetCopyingOptions options)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);

        options.Validate();

        services.AddSingleton(options);
        services.AddScoped<IAssetCopyingService, AwsSqsAssetCopyingService>();
        services.AddAssetBucketPathService();

        services.AddAWSService<IAmazonSQS>();
    }
}