using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using Amazon.S3;
using AspNetCoreRateLimit;
using AssetServer.Shared.AssetCopying;
using AssetStoragePathProviding;
using AuthServer.Permissions;
using AuthServerShared;
using Common.Infrastructure;
using Common.Infrastructure.Aws;
using Common.Infrastructure.BasePath;
using Common.Infrastructure.Caching;
using Common.Infrastructure.CloudFront;
using Common.Infrastructure.Database;
using Common.Infrastructure.EnvironmentInfo;
using Common.Infrastructure.JaegerTracing;
using Common.Infrastructure.Messaging;
using Common.Infrastructure.Middleware;
using Common.Infrastructure.ModerationProvider;
using Common.Infrastructure.MusicProvider;
using Common.Infrastructure.Protobuf;
using Common.Infrastructure.RateLimit;
using Common.Infrastructure.RequestId;
using Common.Infrastructure.ServiceDiscovery;
using Common.Infrastructure.TargetInfoMiddleware;
using Frever.Cache.Configuration;
using Frever.Client.Core.Features.AI.Generation;
using Frever.Client.Core.Features.AI.Metadata;
using Frever.Client.Core.Features.AI.UserGeneratedContent.Characters;
using Frever.Client.Core.Features.AI.UserGeneratedContent.Content;
using Frever.Client.Core.Features.AppStoreApi;
using Frever.Client.Core.Features.CommercialMusic;
using Frever.Client.Core.Features.InAppPurchases;
using Frever.Client.Core.Features.Localizations;
using Frever.Client.Core.Features.MediaFingerprinting;
using Frever.Client.Core.Features.Social;
using Frever.Client.Core.Features.Sounds;
using Frever.Client.Core.Features.Sounds.Playlists;
using Frever.Client.Core.Features.StorageFiles;
using Frever.Client.Shared.ActivityRecording;
using Frever.Client.Shared.AI.Billing;
using Frever.Client.Shared.AI.ComfyUi;
using Frever.Client.Shared.Payouts;
using Frever.ClientService.Api.Features.InAppPurchases;
using Frever.Shared.MainDb;
using Frever.Video.Core.Features.PersonalFeed;
using Frever.Videos.Shared.GeoClusters;
using Frever.Videos.Shared.MusicGeoFiltering;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;

namespace Frever.ClientService.Api;

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
        services.AddRequestIdAccessor();
        services.AddConfiguredAWSOptions(Configuration);
        services.AddJaegerTracing(Configuration);
        services.AddHttpContextAccessor();

        services.AddControllers(
                     options =>
                     {
                         options.OutputFormatters.Add(new ProtobufOutputFormatter());

                         var policy = new AuthorizationPolicyBuilder()
                                     .RequireAuthenticatedUser() // all controllers require authenticated users - if not marked [AllowAnonymous]
                                     .RequireClaim(Claims.PrimaryGroupId)
                                     .RequireClaim(Claims.UserId)
                                     .Build();

                         options.Filters.Add(new AuthorizeFilter(policy));
                     }
                 )
                .AddNewtonsoftJson(configure => configure.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore);

        var serviceUrls = services.AddServiceUrls(Configuration);

        services.AddAuthentication("Bearer")
                .AddIdentityServerAuthentication(
                     options =>
                     {
                         options.Authority = serviceUrls.Auth;
                         options.RequireHttpsMetadata = false;
                         options.ApiName = serviceUrls.AuthApiName;
                     }
                 );

        services.AddCors();
        services.AddHealthChecks();
        services.AddHttpClient();
        services.AddAWSService<IAmazonS3>();

        var envInfo = Configuration.BindEnvironmentInfo();
        services.AddSingleton(envInfo);
        var redisSettings = Configuration.BindRedisSettings();
        services.AddRedis(redisSettings, envInfo.Version);

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

                options.CustomSchemaIds(t => t.ToString());
                var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
            }
        );

        services.AddUserInfo();

        // Business services
        var dbConnectionConfig = Configuration.GetDbConnectionConfiguration();

        services.AddFreverDataWritingAccess(dbConnectionConfig);
        services.AddFreverCachedDataAccess(dbConnectionConfig);
        services.AddFreverPermissions(dbConnectionConfig);
        services.AddFreverCachingCurrentUserAccessor(
            provider =>
            {
                var currentUser = (UserInfo) provider.GetService(typeof(UserInfo));
                return currentUser?.UserMainGroupId;
            }
        );

        var bucketName = Configuration.GetValue<string>("AWS:bucket_name");

        var mediaFingerprintOptions = new MediaFingerprintingOptions();
        Configuration.GetSection("AcrCloud").Bind(mediaFingerprintOptions);
        mediaFingerprintOptions.LogBucket = bucketName;
        mediaFingerprintOptions.Validate();
        services.AddAiGeneratedContent(mediaFingerprintOptions);

        var assetServerSettings = new AssetServerSettings();
        Configuration.Bind("AssetServerSettings", assetServerSettings);

        var inAppPurchaseOptions = new InAppPurchaseOptions();
        Configuration.Bind("InAppPurchases", inAppPurchaseOptions);
        services.AddInAppPurchases(inAppPurchaseOptions);

        var appStoreApiOptions = new AppStoreApiOptions();
        Configuration.Bind("AppStoreApi", appStoreApiOptions);
        services.AddAppStoreApi(appStoreApiOptions);

        var assetCopyingOptions = new AssetCopyingOptions();
        Configuration.Bind("AssetCopying", assetCopyingOptions);
        services.AddAssetCopying(assetCopyingOptions);

        var videoNamingHelperOptions = new VideoNamingHelperOptions
                                       {
                                           DestinationVideoBucket = assetCopyingOptions.BucketName,
                                           CloudFrontHost = Configuration.GetValue<string>("CloudFrontHost"),
                                           IngestVideoBucket = Configuration.GetValue<string>("IngestVideoS3BucketName")
                                       };
        Configuration.Bind(videoNamingHelperOptions);
        videoNamingHelperOptions.Validate();
        services.AddSingleton(videoNamingHelperOptions);
        services.AddSingleton<VideoNamingHelper>();

        services.AddExternalPlaylists();
        services.AddMusicLicenseFiltering(Configuration);
        services.AddMusicProviderApiSettings(Configuration);
        services.AddMusicProviderOAuthSettings(Configuration);
        services.AddCommercialMusic(Configuration);
        services.AddLocalizations();
        services.AddSounds(assetServerSettings);
        services.AddStorageFiles();

        services.AddAiGeneration(Configuration);
        services.AddAiCharacters();
        services.AddAiMetadata();
        services.AddAiBilling(Configuration);
        services.AddComfyUiApi(Configuration);

        services.AddSocialServices(Configuration);
        services.AddUserActivityRecording(Configuration);
        services.AddPayouts(Configuration);
        services.AddGeoCluster();
        services.AddModerationProviderApi(Configuration);
        services.AddSnsMessaging(Configuration);
        services.AddCloudFrontConfiguration(Configuration);
        services.AddPersonalFeed(Configuration);

        services.AddAutoMapper(typeof(Startup), typeof(LocaleMappingProfile));

        services.AddOpenTelemetry()
                .WithMetrics(
                     builder => builder.AddMeter("Frever.ClientService")
                                       .SetResourceBuilder(
                                            ResourceBuilder.CreateDefault()
                                                           .AddService("Frever.ClientService", serviceVersion: envInfo.Version)
                                        )
                                       .AddAspNetCoreInstrumentation()
                                       .AddHttpClientInstrumentation()
                                       .AddPrometheusExporter()
                 );
        ThreadPool.SetMinThreads(120, 200);

        ConfigRateLimit(services);

        services.AddMvc();
    }

    private void ConfigRateLimit(IServiceCollection services)
    {
        var threshold = Configuration.GetSection("RateLimit").Get<RateLimitThreshold>();
        if (!threshold.Enabled)
            return;

        threshold.Validate();

        var limit = threshold.ToSevenDigitalSongDownloadLimitAndPeriod().Item1;
        var period = threshold.ToSevenDigitalSongDownloadLimitAndPeriod().Item2;
        var rateLimitRules = new List<RateLimitRule>
                             {
                                 new() {Endpoint = "*:/MusicProvider/SignUrl", Limit = limit, Period = period},
                                 new() {Endpoint = "*", Limit = int.Parse(threshold.HardLimitPerUserPerHour), Period = "1h"}
                             };

        RateLimitConfig.ConfigRateLimit(services, Configuration, rateLimitRules);
    }


    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseCors(builder => builder.AllowAnyHeader().AllowCredentials().AllowAnyMethod().SetIsOriginAllowed(_ => true));

        app.UseFreverBasePath(Configuration);
        app.UseFreverHealthChecks();
        app.UseRequestId();
        app.UseMiddleware<LoggingMiddlewareService>();
        app.UseTargetInfo();

        app.UseMiddleware<UrlAccessMiddleware>();
        app.UseSwagger();
        app.UseSwaggerUI();

        app.UseOpenTelemetryPrometheusScrapingEndpoint();
        app.UseAuthentication();
        app.UseRouting();
        app.UseAuthorization();

        app.UseMiddleware<ExceptionMiddlewareService>();
        app.UseJaegerRequestInfo();

        var threshold = Configuration.GetSection("RateLimit").Get<RateLimitThreshold>();
        if (threshold.Enabled)
            app.UseMiddleware<AlertingClientRateLimitMiddleware>();

        app.UseEndpoints(
            endpoints => { endpoints.MapControllerRoute("config", "api/config", new {controller = "Config", action = "GetConfig"}); }
        );
    }
}