using System;
using System.IO;
using Common.Infrastructure.Aws.Crypto;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Common.Infrastructure.CloudFront;

public static class ServiceConfiguration
{
    public static void AddCloudFrontConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var cloudFrontConfig = new CloudFrontConfiguration();
        configuration.Bind(cloudFrontConfig);
        cloudFrontConfig.Validate();

        FreverAmazonCloudFrontSigner.Init(new StringReader(cloudFrontConfig.CloudFrontCertPrivateKey.Replace(@"\n", Environment.NewLine)));

        services.AddSingleton(cloudFrontConfig);
    }
}