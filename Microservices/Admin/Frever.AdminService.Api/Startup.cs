using System;
using System.Linq;
using Amazon.MediaConvert;
using Amazon.S3;
using AssetStoragePathProviding;
using AuthServer.DataAccess;
using AuthServer.Permissions;
using AuthServer.TokenGeneration;
using AuthServerShared;
using Common.Infrastructure;
using Common.Infrastructure.Aws;
using Common.Infrastructure.BasePath;
using Common.Infrastructure.Caching;
using Common.Infrastructure.Database;
using Common.Infrastructure.EmailSending;
using Common.Infrastructure.EnvironmentInfo;
using Common.Infrastructure.JaegerTracing;
using Common.Infrastructure.Messaging;
using Common.Infrastructure.Middleware;
using Common.Infrastructure.ModerationProvider;
using Common.Infrastructure.Protobuf;
using Common.Infrastructure.RequestId;
using Common.Infrastructure.ServiceDiscovery;
using Common.Infrastructure.ServiceInfo;
using Common.Infrastructure.TargetInfoMiddleware;
using Frever.AdminService.Api.Infrastructure;
using Frever.AdminService.Core;
using Frever.AdminService.Core.Services.MusicModeration;
using Frever.AdminService.Core.Services.Social;
using Frever.Cache.PubSub;
using Frever.Client.Shared.ActivityRecording;
using Frever.Client.Shared.Social;
using Frever.Shared.MainDb;
using Frever.Videos.Shared.MusicGeoFiltering;
using Microsoft.AspNet.OData.Extensions;
using Microsoft.AspNet.OData.Formatter;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using NotificationService.Client;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using UrlAccessMiddleware = Frever.AdminService.Api.Utils.UrlAccessMiddleware;

#pragma warning disable CA1303

namespace Frever.AdminService.Api;

public class Startup
{
    public IConfiguration Configuration { get; } = new ConfigurationBuilder().AddJsonFile("appsettings.json", true)
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
        services.AddOData();
        services.AddRequestIdAccessor();
        services.AddConfiguredAWSOptions(Configuration);
        services.AddJaegerTracing(Configuration);
        services.AddCors();
        services.AddHttpContextAccessor();

        services.AddControllers(
                     options =>
                     {
                         options.Conventions.Add(new DynamicControllerRouteConvention());

                         foreach (var outputFormatter in options.OutputFormatters.OfType<ODataOutputFormatter>()
                                                                .Where(e => e.SupportedMediaTypes.Count == 0))
                             outputFormatter.SupportedMediaTypes.Add(new MediaTypeHeaderValue("application/prs.odatatestxx-odata"));

                         foreach (var inputFormatter in options.InputFormatters.OfType<ODataInputFormatter>()
                                                               .Where(e => e.SupportedMediaTypes.Count == 0))
                             inputFormatter.SupportedMediaTypes.Add(new MediaTypeHeaderValue("application/prs.odatatestxx-odata"));

                         options.OutputFormatters.Add(new ProtobufOutputFormatter());

                         options.ModelBinderProviders.Insert(0, new EntityUpdateModelBinderProvider());

                         var policy = new AuthorizationPolicyBuilder()
                                     .RequireAuthenticatedUser() // all controllers require authenticated users - if not marked [AllowAnonymous]
                                     .RequireClaim(Claims.PrimaryGroupId)
                                     .RequireClaim(Claims.UserId)
                                     .Build();

                         options.Filters.Add(new AuthorizeFilter(policy));
                     }
                 )
                .ConfigureApplicationPartManager(
                     opts =>
                     {
                         opts.FeatureProviders.Add(new DynamicControllerFeatureProvider());
                         opts.FeatureProviders.Add(new ServiceInfoControllerFeatureProvider());
                     }
                 )
                .AddNewtonsoftJson(configure => configure.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore);

        var urls = services.AddServiceUrls(Configuration);

        services.AddAuthentication("Bearer")
                .AddIdentityServerAuthentication(
                     options =>
                     {
                         options.Authority = urls.Auth;
                         options.RequireHttpsMetadata = false;
                         options.ApiName = urls.AuthApiName;
                     }
                 );

        var dbConnectionConfig = Configuration.GetDbConnectionConfiguration();

        services.AddFreverDataWritingAccess(dbConnectionConfig);
        services.AddFreverDatabaseMigrations(dbConnectionConfig);
        services.AddFreverAuthDbDataAccess(dbConnectionConfig.AuthDb);

        var envInfo = Configuration.BindEnvironmentInfo();
        services.AddSingleton(envInfo);
        var redisSettings = Configuration.BindRedisSettings();
        services.AddRedis(redisSettings, envInfo.Version);

        services.AddEmailSending(Configuration);
        services.AddFreverServices(Configuration);
        services.AddFreverPermissions(dbConnectionConfig);
        services.AddFreverPermissionManagement(dbConnectionConfig);
        services.AddUserActivityRecording(Configuration);
        services.AddSocialSharedService();
        services.AddTokenGeneration();
        services.AddServiceUrls(Configuration);
        services.AddSocialManagement();
        services.AddUserInfo();
        services.AddAssetBucketPathService();

        if (Configuration.IsMigrationRunAllowed())
        {
            using var provider = services.BuildServiceProvider();
            var database = provider.GetService<IMigrator>();

            database.Migrate().Wait();
        }

        services.AddHealthChecks();
        services.AddTargetInfo(Configuration);
        services.AddNotificationServiceClient(Configuration);
        services.AddModerationProviderApi(Configuration);
        services.AddSnsMessaging(Configuration);
        services.AddMusicLicenseFiltering(Configuration);

        services.AddAWSService<IAmazonS3>();
        services.AddAWSService<IAmazonMediaConvert>();

        services.AddOpenTelemetry()
                .WithMetrics(
                     builder => builder.AddMeter("Frever.AdminService")
                                       .SetResourceBuilder(
                                            ResourceBuilder.CreateDefault()
                                                           .AddService("Frever.AdminService", serviceVersion: envInfo.Version)
                                        )
                                       .AddAspNetCoreInstrumentation()
                                       .AddHttpClientInstrumentation()
                                       .AddPrometheusExporter()
                 );

        services.AddRedisSubscribing();
        services.AddRedisSubscriber<DeleteSongRpcListener>();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseCors(builder => builder.AllowAnyHeader().AllowCredentials().AllowAnyMethod().SetIsOriginAllowed(_ => true));

        app.UseFreverBasePath(Configuration);
        app.UseFreverHealthChecks();
        app.UseRequestId();
        app.UseMiddleware<LoggingMiddlewareService>();
        app.UseTargetInfo();

        if (!env.IsProduction() && !env.IsStaging())
            app.UseDeveloperExceptionPage();
        else
            app.UseHsts();

        app.UseMiddleware<UrlAccessMiddleware>();
        app.UseOpenTelemetryPrometheusScrapingEndpoint();
        app.UseAuthentication();
        app.UseRouting();

        app.UseMiddleware<ExceptionMiddlewareService>();
        app.UseMiddleware<CheckUserAdminMiddleware>();
        app.UseJaegerRequestInfo();

        app.UseEndpoints(
            endpoints =>
            {
                endpoints.MapControllerRoute("config", "api/config", new {controller = "Config", action = "GetConfig"});
                endpoints.EnableDependencyInjection();
                endpoints.Expand().Select().MaxTop(100).Count().Expand().Filter().OrderBy();
            }
        );
    }
}