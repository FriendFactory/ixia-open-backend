using System;
using System.Text.Json.Serialization;
using System.Threading;
using Amazon.S3;
using AssetServer.Shared.AssetCopying;
using AuthServerShared;
using Common.Infrastructure;
using Common.Infrastructure.Aws;
using Common.Infrastructure.BasePath;
using Common.Infrastructure.Caching;
using Common.Infrastructure.CloudFront;
using Common.Infrastructure.Database;
using Common.Infrastructure.EnvironmentInfo;
using Common.Infrastructure.JaegerTracing;
using Common.Infrastructure.Middleware;
using Common.Infrastructure.Protobuf;
using Common.Infrastructure.RequestId;
using Common.Infrastructure.TargetInfoMiddleware;
using Frever.Cache.PubSub;
using Frever.Client.Shared.Social;
using Frever.Shared.MainDb;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using NotificationService.Core;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;

namespace NotificationService;

public class Startup
{
    private IConfiguration Configuration { get; } = new ConfigurationBuilder().AddJsonFile("appsettings.json", true)
                                                                              .AddJsonFile(
                                                                                   $"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json",
                                                                                   true
                                                                               )
                                                                              .AddEnvironmentVariables()
                                                                              .AddBeanstalkConfig()
                                                                              .Build();

    // This method gets called by the runtime. Use this method to add services to the container.
    // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
    public void ConfigureServices(IServiceCollection services)
    {
        var appConfig = new AppConfig();
        Configuration.Bind(appConfig);
        appConfig.Validate();
        services.AddSingleton(appConfig);

        var oneSignalConfig = new OneSignalOptions();
        Configuration.GetSection("OneSignal").Bind(oneSignalConfig);
        services.AddSingleton(oneSignalConfig);

        services.AddRequestIdAccessor();
        services.AddHttpClient();
        services.AddUserInfo();
        services.AddHealthChecks();

        var dbConnectionConfig = Configuration.GetDbConnectionConfiguration();
        services.AddFreverDataWritingAccess(dbConnectionConfig);

        services.AddControllers(
            opts =>
            {
                opts.EnableEndpointRouting = false;
                opts.OutputFormatters.Add(new ProtobufOutputFormatter());
            }
        );

        services.AddAuthentication("Bearer")
                .AddIdentityServerAuthentication(
                     options =>
                     {
                         options.Authority = appConfig.Auth.AuthServer;
                         options.RequireHttpsMetadata = false;
                         options.ApiName = appConfig.Auth.ApiName;
                     }
                 );

        services.AddMvc().AddJsonOptions(opts => { opts.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()); });

        var envInfo = Configuration.BindEnvironmentInfo();
        services.AddSingleton(envInfo);
        var redisSettings = Configuration.BindRedisSettings();
        services.AddRedis(redisSettings, envInfo.Version);

        var assetCopyingOptions = new AssetCopyingOptions();
        Configuration.Bind("AssetCopying", assetCopyingOptions);
        services.AddAssetCopying(assetCopyingOptions);

        services.AddConfiguredAWSOptions(Configuration);
        services.AddAWSService<IAmazonS3>();
        services.AddCloudFrontConfiguration(Configuration);

        services.AddNotificationServices();

        services.AddRedisSubscribing();
        services.AddRedisSubscriber<NotificationCacheUpdateListener>();

        services.AddJaegerTracing(Configuration);
        services.AddTargetInfo(Configuration);
        services.AddSwaggerGen(
            options =>
            {
                // add JWT Authentication
                var securityScheme = new OpenApiSecurityScheme
                                     {
                                         Name = "JWT Authentication",
                                         Description = "Enter JWT Bearer token **_only_**",
                                         In = ParameterLocation.Header,
                                         Type = SecuritySchemeType.Http,
                                         Scheme = "bearer", // must be lower case
                                         BearerFormat = "JWT",
                                         Reference = new OpenApiReference
                                                     {
                                                         Id = JwtBearerDefaults.AuthenticationScheme, Type = ReferenceType.SecurityScheme
                                                     }
                                     };
                options.AddSecurityDefinition(securityScheme.Reference.Id, securityScheme);
                options.AddSecurityRequirement(new OpenApiSecurityRequirement {{securityScheme, []}});
            }
        );

        services.AddSocialSharedService();
        services.AddOpenTelemetry()
                .WithMetrics(
                     builder => builder.AddMeter("Frever.NotificationService")
                                       .SetResourceBuilder(
                                            ResourceBuilder.CreateDefault()
                                                           .AddService("Frever.NotificationService", serviceVersion: envInfo.Version)
                                        )
                                       .AddAspNetCoreInstrumentation()
                                       .AddHttpClientInstrumentation()
                                       .AddPrometheusExporter()
                 );
        ThreadPool.SetMinThreads(120, 200);
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseFreverBasePath(Configuration);
        app.UseFreverHealthChecks();

        app.UseRequestId();
        app.UseMiddleware<LoggingMiddlewareService>();
        app.UseMiddleware<ExceptionMiddlewareService>();
        app.UseTargetInfo();
        app.UseJaegerRequestInfo();

        if (env.IsDevelopment())
            app.UseDeveloperExceptionPage();

        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();

        app.UseMiddleware<UrlAccessMiddleware>();
        app.UseSwagger();
        app.UseSwaggerUI();

        app.UseOpenTelemetryPrometheusScrapingEndpoint();
        app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
    }
}