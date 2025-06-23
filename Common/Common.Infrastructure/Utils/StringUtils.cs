using System;
using System.Text;

namespace Common.Infrastructure.Utils;

public static class StringUtils
{
    /// <summary>
    ///     Decodes base64 string performing padding if necessary.
    /// </summary>
    public static string Base64DecodeSafe(this string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(value));

        var paddingLength = 4 - value.Length % 4;
        if (paddingLength < 4)
            value = value.PadRight(value.Length + paddingLength, '=');

        return Encoding.UTF8.GetString(Convert.FromBase64String(value));
    }

    public static string ParseVersion(this string value, int? charsCount = 4)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return value.Length <= charsCount ? value : value[..charsCount!.Value];
    }
}