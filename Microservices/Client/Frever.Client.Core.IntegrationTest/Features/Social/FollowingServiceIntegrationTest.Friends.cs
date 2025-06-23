using FluentAssertions;
using Frever.Client.Core.Features.Social.Followers;
using Frever.Client.Core.IntegrationTest.Utils;
using Frever.Common.IntegrationTesting;
using Frever.Common.IntegrationTesting.Data;
using Frever.Common.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Frever.Client.Core.IntegrationTest.Features.Social;

public class FollowingServiceIntegrationTest(ITestOutputHelper testOut) : IntegrationTestBase
{
    [Fact]
    public async Task Following_HappyPath()
    {
        var services = new ServiceCollection();
        services.AddClientIntegrationTests(testOut);

        await using var provider = services.BuildServiceProvider();
        var dataEnv = provider.GetRequiredService<DataEnvironment>();

        using var firstUserScope = provider.CreateScope();
        var firstUser = await dataEnv.WithUserAndGroup(
                            new UserAndGroupCreateParams {CountryIso3 = "swe", LanguageIso3 = "swe", MainCharacterId = 10}
                        );

        firstUserScope.ServiceProvider.SetCurrentUser(firstUser);

        using var secondUserScope = provider.CreateScope();
        var secondUser = await dataEnv.WithUserAndGroup(
                             new UserAndGroupCreateParams {CountryIso3 = "swe", LanguageIso3 = "swe", MainCharacterId = 20}
                         );
        secondUserScope.ServiceProvider.SetCurrentUser(secondUser);

        var firstUserService = firstUserScope.ServiceProvider.GetRequiredService<IFollowingService>();
        var secondUserService = secondUserScope.ServiceProvider.GetRequiredService<IFollowingService>();

        await firstUserService.FollowGroupAsync(secondUser.MainGroupId);

        {
            var followed = await firstUserService.GetFollowedProfilesAsync(firstUser.MainGroupId, null, 0, 1000);
            followed.Should().HaveCount(1);
            followed[0].MainGroupId.Should().Be(secondUser.MainGroupId);

            var followers = await secondUserService.GetFollowersProfilesAsync(secondUser.MainGroupId, null, 0, 1000);
            followers.Should().HaveCount(1);
            followers[0].MainGroupId.Should().Be(firstUser.MainGroupId);

            var friends = await firstUserService.GetFriendProfilesAsync(
                              firstUser.MainGroupId,
                              null,
                              false,
                              0,
                              1000
                          );
            friends.Should().HaveCount(0);
        }

        await secondUserService.FollowGroupAsync(firstUser.MainGroupId);

        {
            var followed = await secondUserService.GetFollowedProfilesAsync(secondUser.MainGroupId, null, 0, 1000);
            followed.Should().HaveCount(1);
            followed[0].MainGroupId.Should().Be(firstUser.MainGroupId);

            var followers = await firstUserService.GetFollowersProfilesAsync(firstUser.MainGroupId, null, 0, 1000);
            followers.Should().HaveCount(1);
            followers[0].MainGroupId.Should().Be(secondUser.MainGroupId);

            var friends = await firstUserService.GetFriendProfilesAsync(
                              firstUser.MainGroupId,
                              null,
                              false,
                              0,
                              1000
                          );
            friends.Should().HaveCount(1);
        }

        await secondUserService.UnFollowGroupAsync(firstUser.MainGroupId);

        {
            var followed = await firstUserService.GetFollowedProfilesAsync(firstUser.MainGroupId, null, 0, 1000);
            followed.Should().HaveCount(1);
            followed[0].MainGroupId.Should().Be(secondUser.MainGroupId);

            var followers = await secondUserService.GetFollowersProfilesAsync(secondUser.MainGroupId, null, 0, 1000);
            followers.Should().HaveCount(1);
            followers[0].MainGroupId.Should().Be(firstUser.MainGroupId);

            var friends = await firstUserService.GetFriendProfilesAsync(
                              firstUser.MainGroupId,
                              null,
                              false,
                              0,
                              1000
                          );
            friends.Should().HaveCount(0);
        }
    }
}