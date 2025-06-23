using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Common.Infrastructure.Utils;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using OpenTelemetry.Trace;

namespace Common.Infrastructure.JaegerTracing;

public class JaegerRequestInfoMiddleware(RequestDelegate next, IServiceProvider provider, JaegerTracingConfig config)
{
    private readonly JaegerTracingConfig _config = config ?? throw new ArgumentNullException(nameof(config));
    private readonly RequestDelegate _next = next ?? throw new ArgumentNullException(nameof(next));

    [DebuggerStepThrough]
    public async Task InvokeAsync(HttpContext httpContext)
    {
        if (_config.EnableDetailedTracing && _config.EnableTracing)
        {
            using var currentSpan = Tracer.CurrentSpan;
            if (httpContext.Request.Path.ToString().EndsWith("/api/health", StringComparison.OrdinalIgnoreCase))
            {
                await _next(httpContext);
                return;
            }

            if (httpContext.Request.ContentType?.StartsWith("application/json", StringComparison.InvariantCultureIgnoreCase) ?? false)
            {
                httpContext.Request.EnableBuffering();

                var position = httpContext.Request.Body.Position;
                var body = await new StreamReader(httpContext.Request.Body).ReadToEndAsync();
                currentSpan.SetAttribute("http.body", body);

                httpContext.Request.Body.Position = position;
            }

            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var h in httpContext.Request.Headers)
                headers[h.Key] = string.Join<string>(";", h.Value);

            currentSpan.SetAttribute("http.headers", JsonConvert.SerializeObject(headers));

            if (headers.TryGetValue("X-Target-Id", out var xTarget))
                currentSpan.SetAttribute("http.x-target-id", xTarget);

            headers.TryGetValue("X-Session-Id", out var xSessionId);

            if (!string.IsNullOrWhiteSpace(xSessionId))
                currentSpan.SetAttribute("session_id", xSessionId);

            if (headers.TryGetValue("Authorization", out var auth))
            {
                var id = auth.Replace("Bearer", "", StringComparison.OrdinalIgnoreCase).Trim();
                currentSpan.SetAttribute("client_id", id);

                var payloadPart = id.Split('.').ElementAtOrDefault(1);
                if (!string.IsNullOrWhiteSpace(payloadPart))
                {
                    var decodedPayload = payloadPart.Base64DecodeSafe();
                    if (!string.IsNullOrWhiteSpace(decodedPayload))
                    {
                        var userData = JsonConvert.DeserializeObject<UserBasicInfo>(decodedPayload);
                        if (!string.IsNullOrWhiteSpace(userData.PrimaryGroupId))
                        {
                            currentSpan.SetAttribute("group_id", userData.PrimaryGroupId);
                            httpContext.Request.Headers.Append("X-ClientId", userData.PrimaryGroupId);
                        }
                    }
                }
            }

            try
            {
                await _next(httpContext);
            }
            catch (Exception ex)
            {
                currentSpan.SetAttribute("exception", ex.ToString());

                throw;
            }
        }
        else
        {
            await _next(httpContext);
        }
    }
}

public static class JaegerRequestInfoMiddlewareExtensions
{
    public static void UseJaegerRequestInfo(this IApplicationBuilder app)
    {
        app.UseMiddleware<JaegerRequestInfoMiddleware>();
    }
}

public class UserBasicInfo
{
    public string PrimaryGroupId { get; set; }
}