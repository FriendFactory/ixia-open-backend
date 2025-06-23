using System;
using Microsoft.Extensions.Configuration;

namespace Common.Infrastructure.EnvironmentInfo;

public static class EnvironmentInfoConfiguration
{
    public static EnvironmentInfo BindEnvironmentInfo(this IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var version = configuration.GetValue<string>("ApiIdentifier");

        var type = configuration.GetValue<string>("EnvironmentType");

        var environmentInfo = new EnvironmentInfo {Version = version, Type = type};

        environmentInfo.Validate();

        return environmentInfo;
    }
}