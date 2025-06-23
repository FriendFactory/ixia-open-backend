using FluentAssertions;
using Frever.Common.IntegrationTesting;
using Frever.Common.IntegrationTesting.Data;
using Frever.Common.IntegrationTesting.Data.Video;
using Frever.Common.Testing;
using Frever.Shared.MainDb.Entities;
using Frever.Video.Core.Features.ReportInappropriate;
using Frever.Video.Core.IntegrationTest.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Frever.Video.Core.IntegrationTest.Features;

public class ReportInappropriateVideoIntegrationTest(ITestOutputHelper testOut) : IntegrationTestBase
{
    [Fact]
    public async Task ReportVideo_HappyPath()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddVideoIntegrationTests(testOut);

        await using var provider = services.BuildServiceProvider();

        var dataEnv = provider.GetRequiredService<DataEnvironment>();

        var user = await dataEnv.WithUserAndGroup(new UserAndGroupCreateParams {CountryIso3 = "swe", LanguageIso3 = "swe"});
        provider.SetCurrentUser(user);

        var video = (await dataEnv.WithVideo(
                         new VideoInput
                         {
                             LevelId = 3,
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

        var testService = provider.GetRequiredService<IReportInappropriateVideoService>();

        // Act
        var report = await testService.ReportVideo(
                         new ReportInappropriateVideoRequest
                         {
                             Message = "Video contains attempts to proof the Earth is flat", VideoId = video.Id, ReasonId = 1
                         }
                     );

        // Assert
        report.Should().NotBeNull();
        report.Id.Should().NotBe(0);
        report.ReporterGroupId.Should().Be(user.MainGroupId);

        var dbReport = await dataEnv.Db.VideoReport.FirstOrDefaultAsync(r => r.Id == report.Id);
        dbReport.Should().NotBeNull();
        dbReport.Id.Should().Be(report.Id);
        dbReport.Message.Should().Be(report.Message);
        dbReport.VideoId.Should().Be(video.Id);
        dbReport.ReporterGroupId.Should().Be(user.MainGroupId);
    }
}