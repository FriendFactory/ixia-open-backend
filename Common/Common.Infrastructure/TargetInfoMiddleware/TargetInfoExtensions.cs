using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Common.Infrastructure.TargetInfoMiddleware;

public static class TargetInfoExtensions
{
    /// <summary>
    ///     Adds configurable X-Target-Id header to response.
    ///     Useful to identify kubernetes pod processed the request
    /// </summary>
    public static void UseTargetInfo(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.UseMiddleware<Middleware.TargetInfoMiddleware>();
    }

    public static void AddTargetInfo(this IServiceCollection services, string targetId)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton(new TargetInfo {TargetIdentifier = targetId});
    }

    public static void AddTargetInfo(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddTargetInfo(configuration.GetValue<string>("XTargetId"));
    }
}