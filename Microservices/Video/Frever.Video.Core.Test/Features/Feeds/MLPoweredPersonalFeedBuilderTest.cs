using FluentAssertions;
using Frever.Shared.MainDb.Entities;
using Frever.Video.Core.Features.PersonalFeed;
using Frever.Video.Core.Features.PersonalFeed.DataAccess;
using Frever.Video.Core.Features.PersonalFeed.Tracing;
using Frever.Video.Core.Test.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Frever.Video.Core.Test.Features.Feeds;

[Collection("ML Feed Builder")]
public class MLPoweredPersonalFeedBuilderTest(ITestOutputHelper testOut)
{
    [Fact(DisplayName = "👍👎Feed Builder Should Call ML Client")]
    public async Task MLFeedBuilderShouldWork()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTestVideoServices(testOut);

        var repo = new Mock<IPersonalFeedRepository>();
        repo.Setup(r => r.GetVideoViews(It.IsAny<long>())).Returns(Task.FromResult(Enumerable.Empty<VideoView>().BuildMock()));

        var response = new MLServiceResponse
                       {
                           Ok = true,
                           Videos = Enumerable.Range(10, 3)
                                              .Select(
                                                   (id, ordinal) => new MLVideoRef
                                                                    {
                                                                        Id = id,
                                                                        GroupId = id + 100,
                                                                        Source = "mock",
                                                                        SongInfo = [],
                                                                        SortOrder = ordinal
                                                                    }
                                               )
                                              .ToArray()
                       };

        var client = new Mock<IMLServiceClient>();
        client.Setup(c => c.BuildPersonalFeed(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<decimal>()))
              .Returns(Task.FromResult(response));

        await using var provider = services.BuildServiceProvider();

        var feedBuilder = new MLPersonalFeedGenerator(
            repo.Object,
            provider.GetRequiredService<ILoggerFactory>(),
            client.Object,
            new StubPersonalFeedTracerFactory()
        );

        var groupId = 14443;
        var headers = "ex=2,b=2";
        var lon = 23.33m;
        var lat = 60.11m;

        // Act
        var result = await feedBuilder.GenerateFeed(groupId, headers, lon, lat);

        // Assert
        result.Length.Should().Be(response.Videos.Length);

        // ML Server is expected to return videos in reversed order!
        result.Select(r => new {r.Id, r.GroupId}).Should().Equal(response.Videos.Reverse().Select(v => new {v.Id, v.GroupId}).ToArray());

        client.Verify(c => c.BuildPersonalFeed(groupId, headers, lon, lat), Times.Once);
        repo.Verify(r => r.GetVideoViews(groupId), Times.Once);
    }
}