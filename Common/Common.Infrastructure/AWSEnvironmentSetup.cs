using System;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace Common.Infrastructure;

public class AWSEnvironmentSetup
{
    public static IConfigurationRoot Setup()
    {
        var tempConfigBuilder = new ConfigurationBuilder();

        tempConfigBuilder.AddJsonFile(@"C:\Program Files\Amazon\ElasticBeanstalk\config\containerconfiguration", true, true);

        var configuration = tempConfigBuilder.Build();

        var keys = configuration.GetSection("iis:env").GetChildren().Select(pair => pair.Value.Split(new[] {'='}, 2));

        foreach (var k in keys.Where(pair => pair.Length > 1))
        {
            var envKey = k[0];
            var envValue = k[1];
            Environment.SetEnvironmentVariable(envKey, envValue);
        }

        return configuration;
    }
}