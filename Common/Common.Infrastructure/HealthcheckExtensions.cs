using System;
using Microsoft.AspNetCore.Builder;

namespace Common.Infrastructure;

public static class HealthcheckExtensions
{
    /// <summary>
    ///     Adds health checks endpoint at /api/health
    ///     Put this at top of the middleware.
    /// </summary>
    public static void UseFreverHealthChecks(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.UseHealthChecks("/api/health");
    }
}