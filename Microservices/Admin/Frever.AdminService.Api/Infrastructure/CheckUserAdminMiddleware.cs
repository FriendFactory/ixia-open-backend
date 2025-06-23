using System;
using System.Net;
using System.Threading.Tasks;
using AuthServer.Permissions.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

#pragma warning disable CA2007, CA1303

namespace Frever.AdminService.Api.Infrastructure;

public class CheckUserAdminMiddleware(RequestDelegate next)
{
    private readonly RequestDelegate _next = next ?? throw new ArgumentNullException(nameof(next));

    public async Task InvokeAsync(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var userPermissionService = httpContext.RequestServices.GetRequiredService<IUserPermissionService>();
        var isAllowed = await userPermissionService.IsCurrentUserEmployee();

        if (!isAllowed)
        {
            httpContext.Response.StatusCode = (int) HttpStatusCode.Forbidden;
            await httpContext.Response.WriteAsync("Not authorized");
        }
        else
        {
            await _next(httpContext);
        }
    }
}