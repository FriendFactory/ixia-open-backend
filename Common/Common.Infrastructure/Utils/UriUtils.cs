using System;
using System.Linq;

namespace Common.Infrastructure.Utils;

public static class UriUtils
{
    public static string CombineUri(params string[] parts)
    {
        ArgumentNullException.ThrowIfNull(parts);

        return string.Join(
            "/",
            parts.Where(s => !string.IsNullOrWhiteSpace(s))
                 .Select(p => p.TrimStart('/').TrimEnd('/'))
                 .Where(s => !string.IsNullOrWhiteSpace(s))
        );
    }
}