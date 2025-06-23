using System;
using Amazon.S3;
using Frever.Cache.Configuration;
using Frever.Video.Core.Features.PersonalFeed.DataAccess;
using Frever.Video.Core.Features.PersonalFeed.Tracing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Frever.Video.Core.Features.PersonalFeed;

public static class ServiceConfiguration
{
    public static void AddPersonalFeed(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddScoped<IPersonalFeedRepository, EntityFrameworkPersonalFeedRepository>();
        services.AddScoped<IPersonalFeedGenerator, MLPersonalFeedGenerator>();
        services.AddScoped<IMLServiceClient, HttpMLVideoFeedClient>();
        services.AddScoped<IPersonalFeedRefreshingService, MLPersonalFeedRefreshingService>();
        services.AddScoped<IPersonalFeedService, MLPersonalFeedService>();

        var awsS3PersonalFeedTracerOptions = new AwsS3PersonalFeedTracerOptions {Bucket = configuration["AWS:bucket_name"]};
        awsS3PersonalFeedTracerOptions.Validate();
        services.AddSingleton(awsS3PersonalFeedTracerOptions);
        services.AddScoped<IPersonalFeedTracerFactory, AwsS3PersonalFeedTraceFactory>();
        services.AddAWSService<IAmazonS3>();

        services.AddFreverCaching(options => { options.InMemory.Blob<FeaturedUserCacheData>(); });
    }
}