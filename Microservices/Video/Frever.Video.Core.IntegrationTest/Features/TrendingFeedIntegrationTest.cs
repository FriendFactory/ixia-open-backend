using FluentAssertions;
using Frever.Common.IntegrationTesting;
using Frever.Common.IntegrationTesting.Data;
using Frever.Common.IntegrationTesting.Data.Video;
using Frever.Common.Testing;
using Frever.Shared.MainDb.Entities;
using Frever.Video.Core.Features.Feeds.Trending;
using Frever.Video.Core.IntegrationTest.Utils;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Frever.Video.Core.IntegrationTest.Features;

public class TrendingFeedIntegrationTest(ITestOutputHelper testOut) : IntegrationTestBase
{
    [Fact]
    public async Task TrendingFeed_EnsureOk()
    {
        var services = new ServiceCollection();
        services.AddVideoIntegrationTests(testOut);

        await using var provider = services.BuildServiceProvider();

        var dataEnv = provider.GetRequiredService<DataEnvironment>();

        var user = await dataEnv.WithUserAndGroup(new UserAndGroupCreateParams {CountryIso3 = "swe", LanguageIso3 = "swe"});
        provider.SetCurrentUser(user);

        await dataEnv.WithTwoGeoClusters();

        var videoAuthors = await dataEnv.WithUsersAndGroups(
                               Enumerable.Range(0, 50)
                                         .Select(
                                              id => new UserAndGroupCreateParams
                                                    {
                                                        NickName = $"video-author-{id}-{Guid.NewGuid().ToString("N")}",
                                                        CountryIso3 = id < 20 ? "swe" : "usa",
                                                        LanguageIso3 = id < 20 ? "swe" : "eng"
                                                    }
                                          )
                                         .ToArray()
                           );

        var trendingVideoInput = Enumerable.Range(0, 50)
                                           .Select(
                                                (_, index) => new VideoInput
                                                               {
                                                                   GroupId = videoAuthors[index].MainGroupId,
                                                                   ToplistPosition = index + 1,
                                                                   Access = VideoAccess.Public,
                                                                   Language = index < 20 ? "swe" : "eng",
                                                                   Country = index < 20 ? "swe" : "usa",
                                                                   Kpi = new VideoKpiInput
                                                                         {
                                                                             Comments = 10,
                                                                             Likes = 20,
                                                                             Remixes = 30,
                                                                             Shares = 4,
                                                                             Views = 1000,
                                                                             BattlesLost = 1,
                                                                             BattlesWon = 10
                                                                         }
                                                               }
                                            )
                                           .ToArray();

        var trendingVideos = await dataEnv.WithVideo(trendingVideoInput);
        var testService = provider.GetRequiredService<ITrendingVideoFeed>();

        // Act
        var trendingPage1 = await testService.GetTrendingVideos(null, 10);
        var trendingPage2 = await testService.GetTrendingVideos(trendingPage1.Last().Key, 10);
        var trendingPage3 = await testService.GetTrendingVideos(trendingPage2.Last().Key, 10);

        // Assert
        trendingPage1.Length.Should().Be(10);
        trendingPage2.Length.Should().Be(10);
        trendingPage3.Length.Should().BeLessThan(10, "Only 20 trending videos available, third page should be less then page size");

        trendingPage2.First().Id.Should().Be(trendingPage1.Last().Id);
        trendingPage3.First().Id.Should().Be(trendingPage2.Last().Id);

        var expectedTrending = trendingVideos.Where(v => v.Language == "swe" && v.Country == "swe").ToArray();
        var actualTrending = trendingPage1.Concat(trendingPage2).Concat(trendingPage3).DistinctBy(v => v.Id).ToArray();

        expectedTrending.Should().HaveSameCount(actualTrending);
        expectedTrending.Select(v => v.Id).ToArray().Should().BeEquivalentTo(actualTrending.Select(v => v.Id).ToArray());

        await dataEnv.AssertVideoInfo(actualTrending);
    }
}