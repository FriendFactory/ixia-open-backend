using FluentAssertions;
using Frever.Common.IntegrationTesting;
using Frever.Common.IntegrationTesting.Data;
using Frever.Common.IntegrationTesting.Data.Video;
using Frever.Common.Testing;
using Frever.Shared.MainDb.Entities;
using Frever.Video.Core.Features.Hashtags.Feed;
using Frever.Video.Core.IntegrationTest.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Frever.Video.Core.IntegrationTest.Features;

public class HashtagFeedIntegrationTest(ITestOutputHelper testOut) : IntegrationTestBase
{
    [Fact]
    public async Task HashtagFeedVideo_EnsureOk()
    {
        var services = new ServiceCollection();
        services.AddVideoIntegrationTests(testOut);

        await using var provider = services.BuildServiceProvider();

        var dataEnv = provider.GetRequiredService<DataEnvironment>();

        var user = await dataEnv.WithUserAndGroup(new UserAndGroupCreateParams {CountryIso3 = "swe", LanguageIso3 = "swe"});
        provider.SetCurrentUser(user);

        var (swe, usa) = await dataEnv.WithTwoGeoClusters();

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

        string[] hashtags = ["cool", "vibe", "fyp", "girls", "selfie"];

        var hashtagVideoInput = Enumerable.Range(0, 50)
                                          .Select(
                                               (_, index) => new VideoInput
                                                              {
                                                                  GroupId = videoAuthors[index].MainGroupId,
                                                                  Access = VideoAccess.Public,
                                                                  Language = index < 20 ? "swe" : "eng",
                                                                  Country = index < 20 ? "swe" : "usa",
                                                                  Hashtags =
                                                                      new[]
                                                                          {
                                                                              hashtags[index % hashtags.Length],
                                                                              hashtags[index * 3 % hashtags.Length]
                                                                          }.Distinct()
                                                                           .ToArray(),
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

        var hashtagVideos = await dataEnv.WithVideo(hashtagVideoInput);

        var testService = provider.GetRequiredService<IHashtagVideoFeed>();

        var existingHashtags = await dataEnv.Db.Hashtag.AsNoTracking().ToArrayAsync();

        foreach (var ht in existingHashtags)
        {
            // Act
            var hashtagPage1 = await testService.GetHashtagVideoFeed(ht.Id, null, 10);
            var hashtagPage2 = await testService.GetHashtagVideoFeed(ht.Id, hashtagPage1.Last().Key, 10);

            hashtagPage1.Length.Should().BePositive();

            hashtagPage1.Concat(hashtagPage2)
                        .Should()
                        .AllSatisfy(
                             v =>
                             {
                                 v.Hashtags.Should().Match(v => v.Any(ht => ht.Name == ht.Name));
                                 v.Id.Should().Match(id => hashtagVideos.Any(e => e.Id == id));
                             }
                         );
            // Assert
            await dataEnv.AssertVideoInfo(hashtagPage1.Concat(hashtagPage2));
        }
    }
}