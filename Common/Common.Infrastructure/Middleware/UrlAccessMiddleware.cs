using System;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Common.Infrastructure.InternalRequest;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Common.Infrastructure.Middleware;

public class UrlAccessMiddleware(RequestDelegate next, IConfiguration configuration)
{
    private readonly string _swaggerPassword = configuration.GetValue("SwaggerPassword", "frever-swagger-kebabkorv");
    private readonly string _swaggerUserName = configuration.GetValue("SwaggerUserName", "frever");

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/swagger"))
        {
            string authHeader = context.Request.Headers["Authorization"];
            if (authHeader != null && authHeader.StartsWith("Basic "))
            {
                var header = AuthenticationHeaderValue.Parse(authHeader);
                var inBytes = Convert.FromBase64String(header.Parameter);
                var credentials = Encoding.UTF8.GetString(inBytes).Split(':');
                var username = credentials[0];
                var password = credentials[1];
                if (username.Equals(_swaggerUserName) && password.Equals(_swaggerPassword))
                {
                    await next.Invoke(context).ConfigureAwait(false);
                    return;
                }
            }

            context.Response.Headers["WWW-Authenticate"] = "Basic";
            context.Response.StatusCode = (int) HttpStatusCode.Unauthorized;
        }
        else if (context.Request.Path.StartsWithSegments("/metrics"))
        {
            if (!context.IsInternalRequest())
                context.Response.StatusCode = (int) HttpStatusCode.Unauthorized;
            else
                await next.Invoke(context).ConfigureAwait(false);
        }
        else
        {
            await next.Invoke(context).ConfigureAwait(false);
        }
    }
}