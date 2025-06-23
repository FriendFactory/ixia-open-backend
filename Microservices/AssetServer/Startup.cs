using System;
using System.Collections.Generic;
using System.IO;
using Amazon.CloudFront;
using Amazon.S3;
using AspNetCoreRateLimit;
using AssetServer.ModelBinders;
using AssetServer.Services;
using AssetServer.Services.Storage;
using AssetStoragePathProviding;
using AuthServer.Permissions;
using AuthServerShared;
using Common.Infrastructure;
using Common.Infrastructure.Aws;
using Common.Infrastructure.Aws.Crypto;
using Common.Infrastructure.BasePath;
using Common.Infrastructure.Caching;
using Common.Infrastructure.Database;
using Common.Infrastructure.EnvironmentInfo;
using Common.Infrastructure.JaegerTracing;
using Common.Infrastructure.Middleware;
using Common.Infrastructure.RateLimit;
using Common.Infrastructure.ServiceDiscovery;
using Common.Infrastructure.ServiceInfo;
using Common.Infrastructure.TargetInfoMiddleware;
using FluentValidation;
using Frever.Shared.MainDb;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Debug;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

#pragma warning disable CS0618

namespace AssetServer;

public class Startup
{
    // for 3.0 use // public static readonly ILoggerFactory MyLoggerFactory= LoggerFactory.Create(builder => { builder.AddConsole(); });
    public static readonly LoggerFactory MyLoggerFactory = new(new[] {new DebugLoggerProvider()});

    public Startup(IConfiguration configuration)
    {
        Configuration = new ConfigurationBuilder().AddEnvironmentVariables().AddConfiguration(configuration).AddBeanstalkConfig().Build();
    }

    private IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
        var assetServiceOptions = new AssetServiceOptions();

        var assetServiceConfigurationSection = Configuration.GetSection("AssetService");

        if (assetServiceConfigurationSection == null)
            throw new InvalidOperationException("AssetService configuration section is missing");

        assetServiceConfigurationSection.Bind(assetServiceOptions);
        new AssetServiceOptionsValidator().ValidateAndThrow(assetServiceOptions);

        services.AddSingleton(assetServiceOptions);

        FreverAmazonCloudFrontSigner.Init(
            new StringReader(assetServiceOptions.CloudFrontCertificatePrivateKey.Replace(@"\n", Environment.NewLine))
        );

        var serviceUrl = services.AddServiceUrls(Configuration);

        services.AddConfiguredAWSOptions(Configuration);
        services.AddAuthentication("Bearer")
                .AddIdentityServerAuthentication(
                     options =>
                     {
                         options.Authority = serviceUrl.Auth;
                         options.RequireHttpsMetadata = false;
                         options.ApiName = serviceUrl.AuthApiName;
                     }
                 );

        var dbConnectionInfo = Configuration.GetDbConnectionConfiguration();
        services.AddFreverDataWritingAccess(dbConnectionInfo);
        services.AddFreverPermissions(dbConnectionInfo);

        services.AddMvc(
                     options =>
                     {
                         options.EnableEndpointRouting = false;

                         options.ModelBinderProviders.Insert(0, new EntityModelTypeBinderProvider());

                         var policy = new AuthorizationPolicyBuilder()
                                     .RequireAuthenticatedUser() // all controllers require authenticated users - if not marked [AllowAnonymous]
                                     .RequireClaim(Claims.PrimaryGroupId)
                                     .RequireClaim(Claims.UserId)
                                     .Build();

                         options.Filters.Add(new AuthorizeFilter(policy));
                     }
                 )
                .AddNewtonsoftJson(
                     configure =>
                     {
                         configure.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                         configure.SerializerSettings.DefaultValueHandling = DefaultValueHandling.Ignore;
                         configure.SerializerSettings.Converters = new List<JsonConverter> {new StringEnumConverter()};
                     }
                 )
                .ConfigureApplicationPartManager(opts => { opts.FeatureProviders.Add(new ServiceInfoControllerFeatureProvider()); });

        services.AddTransient<IAssetService, AssetService>();
        services.AddTransient<IPermissionService, PermissionService>();
        services.AddScoped<IAssetAccessService, DbAssetAccessService>();

        var bucketName = Configuration.GetValue<string>(ConfigKeys.BUCKET_NAME);

        services.AddSingleton<IStorageService>(p => new StorageService(p.GetService<IAmazonS3>(), bucketName));
        services.AddTransient<IDeleteService, DeleteService>();

        services.AddTransient<IFileUploadService, FileUploadService>();
        services.AddTransient<ICopyFileService, CopyFileService>();
        services.Configure<IISServerOptions>(options => { options.MaxRequestBodySize = int.MaxValue; });
        services.Configure<KestrelServerOptions>(
            options =>
            {
                options.Limits.MaxRequestBodySize = int.MaxValue; // if don't set default value is: 30 MB
            }
        );
        services.Configure<FormOptions>(
            x =>
            {
                x.ValueLengthLimit = int.MaxValue;
                x.MultipartBodyLengthLimit = int.MaxValue; // if don't set default value is: 128 MB
                x.MultipartHeadersLengthLimit = int.MaxValue;
            }
        );

        services.AddSingleton<ICloudFrontService, AwsCloudFrontService>();
        services.AddAWSService<IAmazonS3>();
        services.AddAWSService<IAmazonCloudFront>();
        services.AddAssetBucketPathService();
        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        services.AddUserInfo();
        services.AddSingleton<IConfigsService, ConfigsService>();
        services.AddHealthChecks();

        services.AddJaegerTracing(Configuration);
        services.AddTargetInfo(Configuration);

        var envInfo = Configuration.BindEnvironmentInfo();
        services.AddSingleton(envInfo);
        ConfigRateLimit(services);

        var redisSettings = Configuration.BindRedisSettings();
        services.AddRedis(redisSettings, envInfo.Version);
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
                                 new() {Endpoint = @":/api/Cdn/(?!.*\/Thumbnail\/).*$", Limit = limit, Period = period},
                                 new() {Endpoint = @":/api/CdnLink/(?!.*\/Thumbnail\/).*$", Limit = limit, Period = period},
                                 new() {Endpoint = ".+", Limit = int.Parse(threshold.HardLimitPerUserPerHour), Period = "1h"}
                             };
        RateLimitConfig.ConfigRateLimit(
            services,
            Configuration,
            rateLimitRules,
            true,
            true,
            options => options.EnableRegexRuleMatching = true
        );
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IHostingEnvironment env)
    {
        app.UseCors(builder => builder.AllowAnyHeader().AllowCredentials().AllowAnyMethod().SetIsOriginAllowed(_ => true));

        app.UseFreverBasePath(Configuration);
        app.UseFreverHealthChecks();

        if (env.IsEnvironment("Development") || env.IsEnvironment("Local") || env.IsEnvironment("MyLocal"))
            app.UseDeveloperExceptionPage();
        else
            app.UseHsts();

        app.UseMiddleware<LoggingMiddlewareService>();
        app.UseMiddleware<ExceptionMiddlewareService>();
        app.UseTargetInfo();
        app.UseJaegerRequestInfo();

        app.UseAuthentication();
        var threshold = Configuration.GetSection("RateLimit").Get<RateLimitThreshold>();
        if (threshold.Enabled)
            app.UseMiddleware<AlertingClientRateLimitMiddleware>();
        app.UseMvc();
    }
}