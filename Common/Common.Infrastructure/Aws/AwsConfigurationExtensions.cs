using System;
using System.Collections.Generic;
using System.Linq;
using Amazon;
using Amazon.Extensions.NETCore.Setup;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Common.Infrastructure.Aws;

public static class AwsConfigurationExtensions
{
    public static void AddConfiguredAWSOptions(this IServiceCollection serviceCollection, IConfiguration configuration)
    {
        var extraConfig = new ConfigurationBuilder().AddEnvironmentVariables().AddConfiguration(configuration).AddBeanstalkConfig().Build();

        var region = extraConfig.GetRequiredValue<string>("AWS_REGION");

        serviceCollection.AddDefaultAWSOptions(new AWSOptions {Region = RegionEndpoint.GetBySystemName(region)});
    }

    public static IConfigurationBuilder AddBeanstalkConfig(this IConfigurationBuilder builder)
    {
        const string configFilePath = "C:\\Program Files\\Amazon\\ElasticBeanstalk\\config\\containerconfiguration";
        var config = new ConfigurationBuilder().AddJsonFile(configFilePath, true).Build();

        var section = config.GetSection("iis:env");

        var envVar = new Dictionary<string, string>();

        foreach (var item in section.GetChildren())
        {
            var parts = item.Value.Split("=");
            envVar.Add(parts[0], string.Join("=", parts.Skip(1)));
        }

        return builder.AddInMemoryCollection(envVar);
    }


    private static T GetRequiredValue<T>(this IConfiguration configuration, string key)
    {
        var val = configuration.GetValue<T>(key);
        if (val == null)
            throw new InvalidOperationException($"Configuration value {key} is not found");

        return val;
    }
}