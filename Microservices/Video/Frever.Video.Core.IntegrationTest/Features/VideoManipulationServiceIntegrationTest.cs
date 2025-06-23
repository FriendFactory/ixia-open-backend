using FluentAssertions;
using Frever.Common.IntegrationTesting;
using Frever.Common.IntegrationTesting.Data;
using Frever.Common.IntegrationTesting.Data.Video;
using Frever.Common.Testing;
using Frever.Shared.MainDb.Entities;
using Frever.Video.Core.Features.Feeds.Account;
using Frever.Video.Core.Features.Manipulation;
using Frever.Video.Core.IntegrationTest.Data;
using Frever.Video.Core.IntegrationTest.Utils;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Frever.Video.Core.IntegrationTest.Features;

public class VideoManipulationServiceIntegrationTest(ITestOutputHelper testOut) : IntegrationTestBase
{
    [Fact]
    public async Task ChangeVideoPrivacy()
    {
        var services = new ServiceCollection();
        services.AddVideoIntegrationTests(testOut);

        await using var provider = services.BuildServiceProvider();

        var dataEnv = provider.GetRequiredService<DataEnvironment>();

        var firstUser = await dataEnv.WithUserAndGroup(new UserAndGroupCreateParams {CountryIso3 = "swe", LanguageIso3 = "swe"});
        provider.SetCurrentUser(firstUser);

        var firstUserVideo = (await dataEnv.WithVideo(
                                  new VideoInput
                                  {
                                      GroupId = firstUser.MainGroupId,
                                      Access = VideoAccess.Public,
                                      Language = "swe",
                                      Country = "swe",
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
                              )).First();

        var firstUserService = provider.GetRequiredService<IVideoManipulationService>();

        using var scope = provider.CreateScope();
        var secondUser = await dataEnv.WithUserAndGroup(new UserAndGroupCreateParams {CountryIso3 = "swe", LanguageIso3 = "swe"});
        scope.ServiceProvider.SetCurrentUser(secondUser);
        var controlVideoService = scope.ServiceProvider.GetRequiredService<IAccountVideoFeed>();

        {
            // Set video public, it should be accessible
            await firstUserService.UpdateVideoAccess(firstUserVideo.Id, new UpdateVideoAccessRequest {Access = VideoAccess.Public});
            var accessible = await controlVideoService.GetGroupVideos(firstUser.MainGroupId, null, 100, 0);
            accessible.Should().HaveCount(1);
            accessible[0].Id.Should().Be(firstUserVideo.Id);
        }

        {
            // Set video private, it should be not accessible
            await firstUserService.UpdateVideoAccess(firstUserVideo.Id, new UpdateVideoAccessRequest {Access = VideoAccess.Private});
            var accessible = await controlVideoService.GetGroupVideos(firstUser.MainGroupId, null, 100, 0);
            accessible.Should().HaveCount(0);
        }

        {
            // Set video for followers, should be inaccessible since second doesn't follow first
            await firstUserService.UpdateVideoAccess(firstUserVideo.Id, new UpdateVideoAccessRequest {Access = VideoAccess.ForFollowers});
            var accessible = await controlVideoService.GetGroupVideos(firstUser.MainGroupId, null, 100, 0);
            accessible.Should().HaveCount(0);

            await dataEnv.WithFollowing(
                new FollowingDataEnv.FollowingDataEnvParam {GroupId = secondUser.MainGroupId, FollowsGroupId = firstUser.MainGroupId}
            );

            var accessible2 = await controlVideoService.GetGroupVideos(firstUser.MainGroupId, null, 100, 0);
            accessible2.Should().HaveCount(1);
        }

        {
            // Set video for friends, should be inaccessible until second wouldn't mutually follow
            await firstUserService.UpdateVideoAccess(firstUserVideo.Id, new UpdateVideoAccessRequest {Access = VideoAccess.ForFriends});
            var accessible = await controlVideoService.GetGroupVideos(firstUser.MainGroupId, null, 100, 0);
            accessible.Should().HaveCount(0);

            await dataEnv.WithFollowing(
                new FollowingDataEnv.FollowingDataEnvParam {GroupId = secondUser.MainGroupId, FollowsGroupId = firstUser.MainGroupId}
            );

            var accessible2 = await controlVideoService.GetGroupVideos(firstUser.MainGroupId, null, 100, 0);
            accessible2.Should().HaveCount(0);

            await dataEnv.WithFollowing(
                new FollowingDataEnv.FollowingDataEnvParam
                {
                    GroupId = secondUser.MainGroupId, FollowsGroupId = firstUser.MainGroupId, IsMutual = true
                }
            );

            var accessible3 = await controlVideoService.GetGroupVideos(firstUser.MainGroupId, null, 100, 0);
            accessible3.Should().HaveCount(1);
        }

        {
            // Set video access for tagged, video should not be accessible
            await firstUserService.UpdateVideoAccess(
                firstUserVideo.Id,
                new UpdateVideoAccessRequest {Access = VideoAccess.ForTaggedGroups, TaggedFriendIds = []}
            );

            var accessible = await controlVideoService.GetGroupVideos(firstUser.MainGroupId, null, 100, 0);
            accessible.Should().HaveCount(0);

            await dataEnv.WithFollowing(
                new FollowingDataEnv.FollowingDataEnvParam
                {
                    GroupId = secondUser.MainGroupId, FollowsGroupId = firstUser.MainGroupId, IsMutual = true
                },
                new FollowingDataEnv.FollowingDataEnvParam
                {
                    GroupId = firstUser.MainGroupId, FollowsGroupId = secondUser.MainGroupId, IsMutual = true
                }
            );

            await firstUserService.UpdateVideoAccess(
                firstUserVideo.Id,
                new UpdateVideoAccessRequest
                {
                    Access = VideoAccess.ForTaggedGroups,
                    TaggedFriendIds =
                    [
                        secondUser.MainGroupId
                    ]
                }
            );
            var accessible2 = await controlVideoService.GetGroupVideos(firstUser.MainGroupId, null, 100, 0);
            accessible2.Should().HaveCount(1);
        }
    }
}