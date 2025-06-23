using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Common.Infrastructure.RequestId;

public static class Extensions
{
    public static void AddRequestIdAccessor(this IServiceCollection serviceCollection)
    {
        ArgumentNullException.ThrowIfNull(serviceCollection);

        serviceCollection.AddHttpContextAccessor();
        serviceCollection.AddSingleton<IHeaderAccessor, HttpContextHeaderAccessor>();
    }

    public static void UseRequestId(this IApplicationBuilder app)
    {
        app.UseMiddleware<RequestIdMiddleware>();
    }
}