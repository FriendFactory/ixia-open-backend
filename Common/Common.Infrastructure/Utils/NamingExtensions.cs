using System;

namespace Common.Infrastructure.Utils;

public static class NamingExtensions
{
    public static string ToCamelCase(this string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return value[..1].ToLower() + value[1..];
    }

    public static string ToPascalCase(this string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return value[..1].ToUpperInvariant() + value[1..];
    }
}