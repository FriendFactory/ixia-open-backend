using AuthServer.Permissions.Services;
using AuthServerShared;
using Common.Infrastructure.Caching;
using Common.Infrastructure.Caching.Advanced.SortedList;
using Common.Infrastructure.RequestId;
using FluentAssertions;
using Frever.Client.Shared.Social.Services;
using Frever.Shared.MainDb.Entities;
using Frever.Video.Core.Features.PersonalFeed;
using Frever.Video.Core.Features.PersonalFeed.DataAccess;
using Frever.Video.Core.Features.PersonalFeed.Tracing;
using Frever.Video.Core.Test.Utils;
using Frever.Videos.Shared.MusicGeoFiltering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Frever.Video.Core.Test.Features.Feeds;

[Collection("Personal Feed")]
public class PersonalFeedTest(ITestOutputHelper testOut)
{
    [Fact(DisplayName = "👍👎 General Personal Feed Loading")]
    public async Task GeneralLoading()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTestVideoServices(testOut);

        await using var provider = services.BuildServiceProvider();
        var cache = provider.GetRequiredService<ICache>();
        var currentUser = provider.GetRequiredService<UserInfo>();

        var mlRecommendations = new MLServiceResponse
                                {
                                    Ok = true,
                                    Videos = Enumerable.Range(1, 5)
                                                       .Select(
                                                            i => new MLVideoRef
                                                                 {
                                                                     Id = i,
                                                                     GroupId = i,
                                                                     SongInfo = [],
                                                                     SortOrder = i,
                                                                     Source = "mock"
                                                                 }
                                                        )
                                                       .ToArray()
                                };
        var (testInstance, fypClientMock) = await CreatePersonalFeedService(provider, mlRecommendations);

        // To ensure we test either direct and cached result
        await cache.Server().FlushAllDatabasesAsync();
        for (var i = 0; i < 3; i++)
        {
            // Act
            var (feed, _) = await testInstance.PersonalFeed(currentUser.UserMainGroupId, null, 1000);

            // Assert
            feed.Length.Should().Be(mlRecommendations.Videos.Length);
            feed.Select(f => f.Id).ToArray().Should().Equal(mlRecommendations.Videos.Select(r => r.Id).ToArray());
        }

        fypClientMock.Verify(
            i => i.BuildPersonalFeed(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<decimal>()),
            Times.Once
        );
    }

    [Fact(DisplayName = "👍👍👍👍 Paged Personal Feed Loading")]
    public async Task PageLoading()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTestVideoServices(testOut);

        await using var provider = services.BuildServiceProvider();
        var cache = provider.GetRequiredService<ICache>();
        var currentUser = provider.GetRequiredService<UserInfo>();

        var mlRecommendations = new MLServiceResponse
                                {
                                    Ok = true,
                                    Videos = Enumerable.Range(1, 1000)
                                                       .Select(
                                                            i => new MLVideoRef
                                                                 {
                                                                     Id = i,
                                                                     GroupId = i,
                                                                     SongInfo = [],
                                                                     SortOrder = i,
                                                                     Source = "mock"
                                                                 }
                                                        )
                                                       .ToArray()
                                };
        var (testInstance, fypClientMock) = await CreatePersonalFeedService(provider, mlRecommendations);

        await cache.Server().FlushAllDatabasesAsync();

        var (firstPage, v1) = await testInstance.PersonalFeed(currentUser.UserMainGroupId, null, 10);
        firstPage.Length.Should().Be(10);
        firstPage.Select(f => f.Id).ToArray().Should().Equal(mlRecommendations.Videos.Take(10).Select(r => r.Id).ToArray());

        var (secondPage, v2) = await testInstance.PersonalFeed(currentUser.UserMainGroupId, firstPage.Last().Key, 10);
        secondPage.Length.Should().Be(10);
        secondPage.First().Id.Should().Be(firstPage.Last().Id); // We use last as target key, so it should be included as first element
        secondPage.Skip(1)                                      // Skip item with requested target key
                  .Select(f => f.Id)
                  .ToArray()
                  .Should()
                  .Equal(mlRecommendations.Videos.Skip(10).Take(9).Select(r => r.Id).ToArray());

        fypClientMock.Verify(
            i => i.BuildPersonalFeed(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<decimal>()),
            Times.Once
        );

        v1.Should().Be(v2);
    }

    private async Task<(IPersonalFeedService, Mock<IMLServiceClient>)> CreatePersonalFeedService(
        IServiceProvider provider,
        MLServiceResponse response
    )
    {
        var locationProvider = provider.GetRequiredService<ICurrentLocationProvider>();
        var location = await locationProvider.Get();

        var currentUser = provider.GetRequiredService<UserInfo>();

        var mlClient = new Mock<IMLServiceClient>(MockBehavior.Strict);
        mlClient.Setup(i => i.BuildPersonalFeed(currentUser.UserMainGroupId, It.IsAny<string>(), location.Lon, location.Lat))
                .Returns(Task.FromResult(response));

        var cache = provider.GetRequiredService<ICache>();

        var repo = new Mock<IPersonalFeedRepository>(MockBehavior.Strict);
        repo.Setup(i => i.GetVideoViews(It.IsAny<long>())).Returns(Task.FromResult(Enumerable.Empty<VideoView>().BuildMock()));

        var feedGenerator = new MLPersonalFeedGenerator(
            repo.Object,
            provider.GetRequiredService<ILoggerFactory>(),
            mlClient.Object,
            new StubPersonalFeedTracerFactory()
        );

        var feedRefresher = new MLPersonalFeedRefreshingService(
            cache,
            provider.GetRequiredService<ILogger<MLPersonalFeedService>>(),
            feedGenerator,
            provider.GetRequiredService<ISortedListCache>(),
            provider.GetRequiredService<IHeaderAccessor>()
        );

        var testInstance = new MLPersonalFeedService(
            provider.GetRequiredService<IUserPermissionService>(),
            provider.GetRequiredService<UserInfo>(),
            cache,
            provider.GetRequiredService<ILogger<MLPersonalFeedService>>(),
            locationProvider,
            feedRefresher,
            provider.GetRequiredService<ISocialSharedService>(),
            new FakeVideoLoader(),
            provider.GetRequiredService<ISortedListCache>()
        );

        return (testInstance, mlClient);
    }
}