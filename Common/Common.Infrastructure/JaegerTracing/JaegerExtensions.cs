using System;
using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Common.Infrastructure.JaegerTracing;

public static class JaegerExtensions
{
    public static void AddJaegerTracing(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var enableDetailedTracing = configuration.GetValue<string>("JAEGER_DETAILED_TRACING");
        var isJaegerEnabledValue = configuration.GetValue<object>("JAEGER_ENABLE");
        var isJaegerEnabled = isJaegerEnabledValue != null && bool.Parse(isJaegerEnabledValue.ToString() ?? string.Empty);

        var jaegerTracingConfig = new JaegerTracingConfig
                                  {
                                      EnableTracing = isJaegerEnabled,
                                      EnableDetailedTracing = StringComparer.OrdinalIgnoreCase.Equals("true", enableDetailedTracing ?? "")
                                  };

        services.AddSingleton(jaegerTracingConfig);

        if (!isJaegerEnabled)
            return;

        var serviceName = configuration.GetValue<string>("JAEGER_SERVICE_NAME");
        ActivitySource activity = new(nameof(serviceName));
        services.AddSingleton(activity);

        services.AddOpenTelemetry()
                .WithTracing(
                     tracerBuilder =>
                     {
                         tracerBuilder.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName))
                                      .AddAspNetCoreInstrumentation()
                                      .AddHttpClientInstrumentation()
                                      .AddEntityFrameworkCoreInstrumentation(
                                           options =>
                                           {
                                               options.SetDbStatementForText = true;
                                               options.SetDbStatementForStoredProcedure = true;
                                           }
                                       )
                                      .AddRedisInstrumentation()
                                      .AddOtlpExporter(
                                           options => { options.Endpoint = new Uri(configuration.GetValue<string>("JAEGER_ENDPOINT")); }
                                       );
                     }
                 );
    }
}