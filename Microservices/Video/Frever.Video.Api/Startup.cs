using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using AspNetCoreRateLimit;
using AuthServer.Permissions;
using AuthServerShared;
using Common.Infrastructure;
using Common.Infrastructure.Aws;
using Common.Infrastructure.Aws.Crypto;
using Common.Infrastructure.BasePath;
using Common.Infrastructure.Caching;
using Common.Infrastructure.Database;
using Common.Infrastructure.EmailSending;
using Common.Infrastructure.EnvironmentInfo;
using Common.Infrastructure.JaegerTracing;
using Common.Infrastructure.Middleware;
using Common.Infrastructure.Protobuf;
using Common.Infrastructure.RateLimit;
using Common.Infrastructure.RequestId;
using Common.Infrastructure.ServiceDiscovery;
using Common.Infrastructure.TargetInfoMiddleware;
using Frever.Client.Shared.AI.Billing;
using Frever.Client.Shared.Social;
using Frever.Shared.MainDb;
using Frever.Video.Core;
using Frever.Video.Core.Features;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;

#pragma warning disable CS8602


namespace Frever.Video.Api;

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
    public void ConfigureServices(IServiceCollection services)
    {
        ConfigureServicesCore(services);
    }

    public void ConfigureServicesCore(IServiceCollection services, bool addDbAccess = true)
    {
        var options = new VideoServerOptions();
        Configuration.Bind(options);
        options.Validate();

        FreverAmazonCloudFrontSigner.Init(new StringReader(options.CloudFrontCertPrivateKey.Replace(@"\n", Environment.NewLine)));

        services.AddRequestIdAccessor();
        services.AddHttpClient();
        services.AddConfiguredAWSOptions(Configuration);
        services.AddEmailSending(Configuration);

        services.AddControllers(
            opts =>
            {
                opts.EnableEndpointRouting = false;
                opts.OutputFormatters.Add(new ProtobufOutputFormatter());
            }
        );

        var serviceUrls = services.AddServiceUrls(Configuration);
        services.AddAuthentication("Bearer")
                .AddIdentityServerAuthentication(
                     o =>
                     {
                         o.Authority = serviceUrls.Auth;
                         o.RequireHttpsMetadata = false;
                         o.ApiName = serviceUrls.AuthApiName;
                     }
                 );

        if (addDbAccess)
        {
            var dbConnectionConfig = Configuration.GetDbConnectionConfiguration();

            services.AddFreverDataWritingAccess(dbConnectionConfig);
            services.AddFreverCachedDataAccess(dbConnectionConfig);
            services.AddFreverPermissions(dbConnectionConfig);
            services.AddSocialSharedService();
        }

        var envInfo = Configuration.BindEnvironmentInfo();
        services.AddSingleton(envInfo);
        var redisSettings = Configuration.BindRedisSettings();
        services.AddRedis(redisSettings, envInfo.Version);

        services.AddVideoServices(Configuration);
        services.AddAiBilling(Configuration);

        services.AddMvc().AddNewtonsoftJson(configure => configure.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore);
        services.AddUserInfo();

        services.AddJaegerTracing(Configuration);
        services.AddTargetInfo(Configuration);
        services.AddSwaggerGen(
            o =>
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
                o.AddSecurityDefinition(securityScheme.Reference.Id, securityScheme);
                o.AddSecurityRequirement(new OpenApiSecurityRequirement {{securityScheme, []}});
            }
        );
        services.AddHealthChecks();
        services.AddOpenTelemetry()
                .WithMetrics(
                     builder => builder.AddMeter("Frever.VideoServer")
                                       .SetResourceBuilder(
                                            ResourceBuilder.CreateDefault()
                                                           .AddService("Frever.VideoServer", serviceVersion: envInfo.Version)
                                        )
                                       .AddAspNetCoreInstrumentation()
                                       .AddHttpClientInstrumentation()
                                       .AddPrometheusExporter()
                 );
        ThreadPool.SetMinThreads(120, 200);
        ConfigRateLimit(services);
    }

    private void ConfigRateLimit(IServiceCollection services)
    {
        var threshold = Configuration.GetSection("RateLimit").Get<RateLimitThreshold>();
        if (!threshold.Enabled)
            return;
        threshold.Validate();
        var limit = threshold.ToFreverVideoAndAssetDownloadLimitAndPeriod().Item1;
        var period = threshold.ToFreverVideoAndAssetDownloadLimitAndPeriod().Item2;
        var rateLimitRules = new List<RateLimitRule>
                             {
                                 new() {Endpoint = "get:/video/play", Limit = limit, Period = period},
                                 new() {Endpoint = "get:/video/template/\\d+", Limit = limit, Period = period},
                                 new() {Endpoint = "get:/video/hashtag/\\d+", Limit = limit, Period = period},
                                 new() {Endpoint = "get:/video/trending", Limit = limit, Period = period},
                                 new() {Endpoint = "get:/video/my-videos", Limit = limit, Period = period},
                                 new() {Endpoint = "get:/video/my-friends-videos", Limit = limit, Period = period},
                                 new() {Endpoint = "get:/video/my-following", Limit = limit, Period = period},
                                 new() {Endpoint = "get:/video/fyp", Limit = limit, Period = period},
                                 new() {Endpoint = "get:/video/by-task/\\d+", Limit = limit, Period = period},
                                 new() {Endpoint = "get:/video/by-group/\\d+", Limit = limit, Period = period},
                                 new() {Endpoint = "get:/video/by-group/\\d+/tasks", Limit = limit, Period = period},
                                 new() {Endpoint = "get:/video/tagged/\\d+", Limit = limit, Period = period},
                                 new() {Endpoint = "get:/video/\\d+/remixes", Limit = limit, Period = period},
                                 new() {Endpoint = "get:/video/featured", Limit = limit, Period = period},
                                 new() {Endpoint = "get:/video/my-videos/by-level/\\d+", Limit = limit, Period = period},
                                 new() {Endpoint = "get:/video/\\d+/view", Limit = limit, Period = period},
                                 new() {Endpoint = "get:/video/\\d+/player-url", Limit = limit, Period = period},
                                 new() {Endpoint = "get:/video/\\d+/file-url", Limit = limit, Period = period},
                                 new() {Endpoint = "get:/video/\\d+", Limit = limit, Period = period},
                                 new() {Endpoint = ".+", Limit = int.Parse(threshold.HardLimitPerUserPerHour), Period = "1h"}
                             };
        RateLimitConfig.ConfigRateLimit(
            services,
            Configuration,
            rateLimitRules,
            false,
            false,
            options => options.EnableRegexRuleMatching = true
        );
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseFreverBasePath(Configuration);
        app.UseFreverHealthChecks();

        app.UseCors(builder => builder.AllowAnyHeader().AllowCredentials().AllowAnyMethod().SetIsOriginAllowed(_ => true));
        app.UseRequestId();
        app.UseMiddleware<LoggingMiddlewareService>();
        app.UseMiddleware<ExceptionMiddlewareService>();
        app.UseTargetInfo();
        app.UseJaegerRequestInfo();

        if (env.IsDevelopment())
            app.UseDeveloperExceptionPage();

        app.UseMiddleware<UrlAccessMiddleware>();
        app.UseOpenTelemetryPrometheusScrapingEndpoint();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();

        app.UseSwagger();
        app.UseSwaggerUI();
        var threshold = Configuration.GetSection("RateLimit").Get<RateLimitThreshold>();
        if (threshold.Enabled)
            app.UseMiddleware<AlertingClientRateLimitMiddleware>();

        app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
    }
}