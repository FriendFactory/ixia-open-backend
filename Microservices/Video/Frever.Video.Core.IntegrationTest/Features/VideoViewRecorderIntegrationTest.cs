using FluentAssertions;
using Frever.Common.IntegrationTesting;
using Frever.Common.IntegrationTesting.Data;
using Frever.Common.IntegrationTesting.Data.Video;
using Frever.Common.Testing;
using Frever.Shared.MainDb.Entities;
using Frever.Video.Contract;
using Frever.Video.Core.Features.Views;
using Frever.Video.Core.IntegrationTest.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Frever.Video.Core.IntegrationTest.Features;

public class VideoViewRecorderIntegrationTest(ITestOutputHelper testOut) : IntegrationTestBase
{
    [Fact]
    public async Task RecordVideoViews_HappyPath()
    {
        var services = new ServiceCollection();
        services.AddVideoIntegrationTests(testOut);

        await using var provider = services.BuildServiceProvider();

        var dataEnv = provider.GetRequiredService<DataEnvironment>();

        var user = await dataEnv.WithUserAndGroup(new UserAndGroupCreateParams {CountryIso3 = "swe", LanguageIso3 = "swe"});
        provider.SetCurrentUser(user);

        var video = (await dataEnv.WithVideo(
                         new VideoInput
                         {
                             GroupId = user.MainGroupId,
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

        var otherUsers = await dataEnv.WithUsersAndGroups(
                             Enumerable.Range(0, 10)
                                       .Select(_ => new UserAndGroupCreateParams {CountryIso3 = "swe", LanguageIso3 = "swe"})
                                       .ToArray()
                         );

        foreach (var u in otherUsers)
        {
            using var scope = provider.CreateScope();
            scope.ServiceProvider.SetCurrentUser(u);

            var videoRecordService = scope.ServiceProvider.GetRequiredService<IVideoViewRecorder>();
            await videoRecordService.RecordVideoView(
                [
                    new ViewViewInfo
                    {
                        FeedTab = "tab1",
                        FeedType = "personal-feed",
                        VideoId = video.Id,
                        ViewDate = DateTime.UtcNow
                    }
                ]
            );
        }

        var views = await dataEnv.Db.VideoView.ToArrayAsync();
        views.Should().HaveCount(otherUsers.Length);
        views.Should()
             .AllSatisfy(
                  v =>
                  {
                      v.VideoId.Should().Be(video.Id);
                      v.UserId.Should().NotBe(0);
                      v.FeedTab.Should().Be("tab1");
                      v.FeedType.Should().Be("personal-feed");
                  }
              );
    }
}