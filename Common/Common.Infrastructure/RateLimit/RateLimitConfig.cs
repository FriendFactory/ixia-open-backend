using System;
using System.Collections.Generic;
using AspNetCoreRateLimit;
using AspNetCoreRateLimit.Redis;
using Common.Infrastructure.Caching;
using Common.Infrastructure.EmailSending;
using Common.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using StackExchange.Redis;

namespace Common.Infrastructure.RateLimit;

public static class RateLimitConfig
{
    private static readonly JsonSerializerSettings JsonSerializerSettings =
        new() {ContractResolver = new CamelCasePropertyNamesContractResolver()};

    public static void ConfigRateLimitForAuth(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ClientRateLimitOptions>(
            options =>
            {
                options.EnableEndpointRateLimiting = true;
                options.StackBlockedRequests = true;
                options.ClientIdHeader = "X-Forwarded-For";
                // Frever office public IP
                options.ClientWhitelist = ["151.236.206.228"];
                options.DisableRateLimitHeaders = true;
                options.GeneralRules =
                [
                    new RateLimitRule
                    {
                        Endpoint = "post:/api/verify-phone-number",
                        Limit = 5,
                        Period = "15m",
                        QuotaExceededResponse = new QuotaExceededResponse
                                                {
                                                    ContentType = "application/json",
                                                    Content = $"{{{JsonConvert.SerializeObject(
                                                        new
                                                        {
                                                            ErrorCode = ErrorCodes.Auth.PhoneNumberTooManyRequests,
                                                            ErrorMessage =
                                                                "Oops! It looks like there have been too many sign-ups from this " +
                                                                "device with phone numbers recently. Try again later or sign up " +
                                                                "with email instead."
                                                        },
                                                        JsonSerializerSettings
                                                    )}}}"
                                                }
                    },
                    new RateLimitRule
                    {
                        Endpoint = "post:/account/RegisterTemporaryAccount",
                        Limit = 5,
                        Period = "5m",
                        QuotaExceededResponse = new QuotaExceededResponse
                                                {
                                                    ContentType = "application/json",
                                                    Content = $"{{{JsonConvert.SerializeObject(
                                                        new
                                                        {
                                                            ErrorCode = ErrorCodes.Auth.AccountRegistrationTooManyRequests,
                                                            ErrorMessage = "Too many requests. Please try again later"
                                                        },
                                                        JsonSerializerSettings
                                                    )}}}"
                                                }
                    }
                ];
            }
        );

        services.AddDistributedRateLimiting<RedisProcessingStrategy>();
        services.AddRedisRateLimiting();

        services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
        services.AddSingleton<IClientPolicyStore, DistributedCacheClientPolicyStore>();
        services.AddSingleton<IRateLimitCounterStore, DistributedCacheRateLimitCounterStore>();

        services.AddEmailSending(configuration);
    }

    public static void ConfigRateLimit(
        IServiceCollection services,
        IConfiguration configuration,
        List<RateLimitRule> rules,
        bool addRedis = false,
        bool addEmailSending = false,
        Action<ClientRateLimitOptions> configure = null
    )
    {
        if (addRedis)
        {
            var redisSettings = configuration.BindRedisSettings();
            redisSettings.Validate();
            services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisSettings.ConnectionString));
        }

        services.Configure<ClientRateLimitOptions>(
            options =>
            {
                options.EnableEndpointRateLimiting = true;
                options.StackBlockedRequests = true;
                options.ClientIdHeader = "X-ClientId";
                options.QuotaExceededMessage = "Too many requests, please try again later.";
                options.DisableRateLimitHeaders = true;
                options.GeneralRules = rules;
                if (configure != null)
                    configure(options);
            }
        );

        services.AddDistributedRateLimiting<RedisProcessingStrategy>();
        services.AddRedisRateLimiting();

        services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
        services.AddSingleton<IClientPolicyStore, DistributedCacheClientPolicyStore>();
        services.AddSingleton<IRateLimitCounterStore, DistributedCacheRateLimitCounterStore>();

        if (addEmailSending)
            services.AddEmailSending(configuration);
    }
}