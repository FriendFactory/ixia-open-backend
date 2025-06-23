using System;
using Frever.Video.Core.Features.Feeds.Account;
using Frever.Video.Core.Features.Feeds.AiContent;
using Frever.Video.Core.Features.Feeds.Featured;
using Frever.Video.Core.Features.Feeds.Remixes;
using Frever.Video.Core.Features.Feeds.TaggedIn;
using Frever.Video.Core.Features.Feeds.Trending;
using Frever.Video.Core.Features.Feeds.UserSounds;
using Microsoft.Extensions.DependencyInjection;

namespace Frever.Video.Core.Features.Feeds;

public static class ServiceConfiguration
{
    public static void AddVideoFeeds(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IUserSoundFeedRepository, PersistentUserSoundFeedRepository>();
        services.AddScoped<IUserSoundVideoFeed, UserSoundVideoFeed>();

        services.AddScoped<ITrendingVideoFeedRepository, PersistentTrendingVideoFeedRepository>();
        services.AddScoped<ITrendingVideoFeed, TrendingVideoFeed>();

        services.AddScoped<IFeaturedVideoFeedRepository, PersistentFeaturedVideoFeedRepository>();
        services.AddScoped<IFeaturedVideoFeed, FeaturedVideoFeed>();

        services.AddScoped<IRemixesOfVideoRepository, PersistentRemixesOfVideoRepository>();
        services.AddScoped<IRemixesOfVideoFeed, RemixesOfVideoFeed>();

        services.AddScoped<ITaggedInVideoRepository, PersistentTaggedInVideoRepository>();
        services.AddScoped<ITaggedInVideoFeed, TaggedInVideoFeed>();

        services.AddScoped<IAccountVideoFeedRepository, PersistentAccountVideoFeedRepository>();
        services.AddScoped<IAccountVideoFeed, AccountVideoFeed>();

        services.AddScoped<IAiContentVideoFeed, AiContentVideoFeed>();
    }
}