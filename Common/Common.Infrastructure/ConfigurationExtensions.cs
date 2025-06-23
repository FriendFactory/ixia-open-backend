using System;
using Microsoft.Extensions.Configuration;

namespace Common.Infrastructure;

public static class ConfigurationExtensions
{
    public static bool IsMigrationRunAllowed(this IConfiguration configuration)
    {
        var value = configuration.GetValue<string>("RunMigrations");

        return !string.IsNullOrWhiteSpace(value) && StringComparer.OrdinalIgnoreCase.Equals(value, "true");
    }
}