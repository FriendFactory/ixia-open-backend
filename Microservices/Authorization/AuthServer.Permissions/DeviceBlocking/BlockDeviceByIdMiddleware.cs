using System;
using System.Linq;
using System.Threading.Tasks;
using Common.Infrastructure;
using Common.Infrastructure.RequestId;
using Frever.Shared.MainDb;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AuthServer.Permissions.DeviceBlocking;

public class BlockDeviceByIdMiddleware(RequestDelegate next, IServiceProvider provider, ILogger<BlockDeviceByIdMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext httpContext)
    {
        var deviceId = httpContext.Request.Headers[HttpContextHeaderAccessor.XDeviceId].FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(deviceId))
        {
            using var scope = provider.CreateScope();

            var db = scope.ServiceProvider.GetRequiredService<WriteDbContext>();
            if (await db.DeviceBlacklist.AnyAsync(d => d.DeviceId == deviceId))
                throw AppErrorWithStatusCodeException.NotAuthorized("You're not permitted to access the service", "NotAuthorized");

            using (logger.BeginScope($" << DeviceId={deviceId} >> "))
            {
                await next(httpContext);
            }
        }
        else
        {
            await next(httpContext);
        }
    }
}

public static class BlockedDeviceServiceConfiguration
{
    public static void UseDeviceBlocking(this IApplicationBuilder app)
    {
        app.UseMiddleware<BlockDeviceByIdMiddleware>();
    }
}