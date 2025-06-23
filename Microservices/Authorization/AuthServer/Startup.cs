// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using AuthServer.Contracts;
using AuthServer.Data;
using AuthServer.Features.CredentialUpdate;
using AuthServer.Models;
using AuthServer.Permissions;
using AuthServer.Permissions.DeviceBlocking;
using AuthServer.Repositories;
using AuthServer.Services;
using AuthServer.Services.AppleAuth;
using AuthServer.Services.BridgeSDK;
using AuthServer.Services.EmailAuth;
using AuthServer.Services.GoogleAuth;
using AuthServer.Services.PasswordAuth;
using AuthServer.Services.PhoneNumberAuth;
using AuthServer.Services.SmsSender;
using AuthServer.Services.TokenGeneration;
using AuthServer.Services.UserManaging;
using AuthServer.Services.UserManaging.NicknameSuggestion;
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
using Common.Infrastructure.RateLimit;
using Common.Infrastructure.ServiceDiscovery;
using Common.Infrastructure.TargetInfoMiddleware;
using FluentValidation;
using Frever.Cache.PubSub;
using Frever.Shared.AssetStore;
using Frever.Shared.MainDb;
using Frever.Videos.Shared.MusicGeoFiltering;
using IdentityServer4.EntityFramework.Stores;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using IdentityServer4.Validation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

//using Microsoft.Extension.Logging;
//using Microsoft.Extension.Logging.Debug;

// Run this command to apply DB migrations:
// env "Auth.Connection"="Server=127.0.0.1;Port=5432;Database=<auth-server-db>;User Id=root;Password=;" "MainDb.Connection"="Server=127.0.0.1;Port=5432;Database=<main-db>;User Id=root;Password=;" dotnet ef database update --context ApplicationDbContext
namespace AuthServer;

public class Startup
{
    public Startup()
    {
        Configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json")
                                                  .AddJsonFile(
                                                       $"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json",
                                                       true
                                                   )
                                                  .AddEnvironmentVariables()
                                                  .AddCommandLine(Environment.GetCommandLineArgs())
                                                  .AddBeanstalkConfig()
                                                  .Build();
    }

    public IConfiguration Configuration { get; }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddConfiguredAWSOptions(Configuration);
        services.AddEmailSending(Configuration);

        var migrationsAssembly = typeof(Startup).GetTypeInfo().Assembly.GetName().Name;
        JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
        JwtSecurityTokenHandler.DefaultOutboundClaimTypeMap.Clear();

        var dbConnectionConfig = Configuration.GetDbConnectionConfiguration();
        services.AddDbContext<ApplicationDbContext>(
            options =>
            {
                options.UseNpgsql(dbConnectionConfig.AuthDb, sql => sql.MigrationsAssembly(migrationsAssembly));
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            }
        );

        services.AddFreverDataWritingAccess(dbConnectionConfig);
        services.AddFreverPermissions(dbConnectionConfig);
        services.AddAssetStoreTransactions();
        services.AddModerationProviderApi(Configuration);
        services.AddSnsMessaging(Configuration);

        var envInfo = Configuration.BindEnvironmentInfo();
        services.AddSingleton(envInfo);
        var redisSettings = Configuration.BindRedisSettings();
        services.AddRedis(redisSettings, envInfo.Version);

        services.AddRedisPublishing();
        services.AddRedisSubscribing();
        services.AddRedisSubscriber<TokenGenerationRpcListener>();

        services.AddHealthChecks();
        services.AddHttpClient();

        services.AddIdentity<ApplicationUser, IdentityRole>(
                     options =>
                     {
                         options.Password.RequireDigit = false;
                         options.Password.RequiredLength = 6;
                         options.Password.RequiredUniqueChars = 0;
                         options.Password.RequireLowercase = false;
                         options.Password.RequireNonAlphanumeric = false;
                         options.Password.RequireUppercase = false;
                     }
                 )
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddEntityFrameworkStores<WriteDbContext>()
                .AddDefaultTokenProviders()
                .AddUserManager<CustomUserManager>();

        services.AddMvc(opts => { opts.EnableEndpointRouting = false; });

        services.Configure<IISOptions>(
            iis =>
            {
                iis.AuthenticationDisplayName = "Windows";
                iis.AutomaticAuthentication = false;
            }
        );

        var onboardingOptions = new OnboardingOptions();
        Configuration.Bind(nameof(OnboardingOptions), onboardingOptions);
        onboardingOptions.Validate();
        services.AddSingleton(onboardingOptions);

        var allowUniversalOtp = Configuration.GetValue<string>("PhoneNumberAuth:AllowUniversalOTP");

        var phoneNumberAuthSettings = new PhoneNumberTokenGrantValidatorSettings
                                      {
                                          AllowUniversalOTP = StringComparer.OrdinalIgnoreCase.Equals(allowUniversalOtp, "true")
                                      };
        services.AddSingleton(phoneNumberAuthSettings);

        var identityServerConfig = new IdentityServerConfiguration();
        Configuration.GetSection(nameof(IdentityServerConfiguration)).Bind(identityServerConfig);
        identityServerConfig.Validate();

        var clients = Config.GetClients(identityServerConfig.ClientSecret, identityServerConfig.AllowedRedirectUrls).ToArray();

        identityServerConfig.ClientId = clients.FirstOrDefault()?.ClientId;
        services.AddSingleton(identityServerConfig);

        var cert = new X509Certificate2(
            Convert.FromBase64String(identityServerConfig.CertificateContentBase64),
            identityServerConfig.CertificatePassword,
            X509KeyStorageFlags.Exportable
        );

        if (cert == null)
            throw new Exception("No cert");

        var builder = services.AddIdentityServer(
                                   options =>
                                   {
                                       var issuerUrl = identityServerConfig.IssuerUrl;

                                       if (!string.IsNullOrWhiteSpace(issuerUrl))
                                       {
                                           options.IssuerUri = issuerUrl;
                                           options.PublicOrigin = issuerUrl;
                                       }

                                       options.Events.RaiseErrorEvents = true;
                                       options.Events.RaiseInformationEvents = true;
                                       options.Events.RaiseFailureEvents = true;
                                       options.Events.RaiseSuccessEvents = true;
                                   }
                               )
                              .AddExtensionGrantValidator<PhoneNumberTokenGrantValidator>()
                              .AddExtensionGrantValidator<AppleAuthExtensionGrantValidator>()
                              .AddExtensionGrantValidator<GoogleAuthExtensionGrantValidator>()
                              .AddExtensionGrantValidator<EmailAuthExtensionGrantValidator>()
                              .AddSigningCredential(cert)
                              .AddInMemoryIdentityResources(Config.GetIdentityResources())
                              .AddInMemoryApiResources(Config.GetApis())
                              .AddInMemoryClients(clients)
                              .AddAspNetIdentity<ApplicationUser>()
                              .AddResourceOwnerValidator<ResourceOwnerPasswordValidator>()
                              .AddCustomTokenRequestValidator<ExtendTokenRequestValidator>()
                              .AddOperationalStore(
                                   option =>
                                   {
                                       option.ConfigureDbContext = b => b.UseNpgsql(
                                                                       dbConnectionConfig.AuthDb,
                                                                       sql => sql.MigrationsAssembly(migrationsAssembly)
                                                                   );
                                   }
                               );

        builder.Services.AddTransient<IJwtTokenProvider, HttpJwtTokenProvider>();
        builder.Services.AddScoped<IUserRepository, UserRepository>();
        builder.Services.AddScoped<IUserAccountService, UserAccountService>();
        builder.Services.AddScoped<ICredentialValidateService, CredentialValidateService>();

        builder.Services.AddScoped<INicknameSuggestionRepository, PersistentNicknameSuggestionRepository>();
        builder.Services.AddScoped<INicknameSuggestionData, HardcodedNicknameSuggestionData>();
        builder.Services.AddScoped<INicknameSuggestionService, NicknameSuggestionService>();

        builder.Services.AddSingleton<IClientVersionService>(_ => new ClientVersionService(Configuration));
        builder.Services.AddTransient<IClaimsService, CustomClaimsService>();
        builder.Services.AddTransient<IResourceOwnerPasswordValidator, ResourceOwnerPasswordValidator>();
        builder.Services.AddTransient<IPersistedGrantStore, PersistedGrantStore>();
        // builder.Services.AddScoped<IPhoneNumberAuthService, PhoneNumberAuthService>();
        builder.Services.AddScoped<IPhoneNumberAuthService, TwilioVerifyPhoneNumberAuthService>();
        builder.Services.AddScoped<IAppleAuthService, AppleAuthService>();
        builder.Services.AddScoped<IGoogleAuthService, GoogleAuthService>();
        builder.Services.AddScoped<IEmailAuthService, EmailAuthService>();
        builder.Services.AddScoped<IValidator<AuthenticationInfo>, AuthenticationInfoValidator>();
        builder.Services.AddScoped<IValidator<AppleEmailInfoRequest>, AppleEmailInfoRequestValidator>();
        builder.Services.AddScoped<IValidator<TemporaryAccountRequest>, TemporaryAccountRequestValidator>();
        builder.Services.AddScoped<IValidator<UpdateAccountRequest>, UpdateAccountRequestValidator>();
        builder.Services.AddScoped<IValidator<VerifyPhoneNumberRequest>, VerifyPhoneNumberRequestValidator>();
        builder.Services.AddScoped<IValidator<VerifyEmailRequest>, VerifyEmailRequestValidator>();

        builder.Services.AddCredentialUpdateService();

        services.AddAuthentication("Bearer")
                .AddJwtBearer(
                     "Bearer",
                     options =>
                     {
                         options.RequireHttpsMetadata = false;
                         options.Audience = "friends_factory.creators_api";

                         options.TokenValidationParameters = new TokenValidationParameters
                                                             {
                                                                 ValidateIssuer = false,
                                                                 ClockSkew = TimeSpan.Zero,
                                                                 NameClaimType = ClaimTypes.Name,
                                                                 RoleClaimType = ClaimTypes.Role,
                                                                 RequireSignedTokens = true,
                                                                 IssuerSigningKey = new X509SecurityKey(cert),
                                                                 IssuerSigningKeyResolver =
                                                                     (_, _, _, _) => new List<X509SecurityKey> {new(cert)}
                                                             };
                     }
                 );

        builder.Services.AddServiceUrls(Configuration);
        builder.Services.AddTwilio(Configuration);
        builder.Services.AddJaegerTracing(Configuration);
        builder.Services.AddTargetInfo(Configuration);

        services.AddMusicLicenseFiltering(Configuration);

        services.AddScoped<ITokenGenerationService, TokenGenerationService>();
        ConfigRateLimit(services);
    }

    private void ConfigRateLimit(IServiceCollection services)
    {
        var threshold = Configuration.GetSection("RateLimit").Get<RateLimitThreshold>();
        if (!threshold.Enabled)
            return;

        RateLimitConfig.ConfigRateLimitForAuth(services, Configuration);
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsEnvironment("Development") || env.IsEnvironment("Local") || env.IsEnvironment("MyLocal"))
            app.UseDeveloperExceptionPage();

        app.UseFreverBasePath(Configuration);
        app.UseFreverHealthChecks();

        app.UseStaticFiles();
        app.UseCors(builder => { builder.AllowAnyHeader().AllowCredentials().AllowAnyMethod().SetIsOriginAllowed(_ => true); });

        app.UseMiddleware<LoggingMiddlewareService>();
        app.UseMiddleware<ExceptionMiddlewareService>();
        app.UseTargetInfo();
        app.UseJaegerRequestInfo();
        app.UseDeviceBlocking();
        app.UseIdentityServer();
        var threshold = Configuration.GetSection("RateLimit").Get<RateLimitThreshold>();
        if (threshold.Enabled)
            app.UseMiddleware<AlertingClientRateLimitMiddleware>();
        app.UseMvcWithDefaultRoute();
    }
}