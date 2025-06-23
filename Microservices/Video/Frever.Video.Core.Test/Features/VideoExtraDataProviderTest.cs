using AssetStoragePathProviding;
using AuthServerShared;
using FluentAssertions;
using Frever.Client.Shared.Social.Services;
using Frever.ClientService.Contract.Social;
using Frever.Shared.MainDb.Entities;
using Frever.Video.Contract;
using Frever.Video.Core.Features.AssetUrlGeneration;
using Frever.Video.Core.Features.VideoInfoExtraData;
using Frever.Video.Core.Features.VideoInformation.DataAccess;
using Frever.Video.Core.Test.Utils;
using Frever.Videos.Shared.CachedVideoKpis;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Frever.Video.Core.Test.Features;

[Collection("VideoInfo Extra Data")]
public class VideoExtraDataProviderTest(ITestOutputHelper testOut)
{
    [Fact(DisplayName = "👍👎 Setting general info works")]
    public async Task EnsureSetExtraDataWorks()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTestVideoServices(testOut);
        await using var provider = services.BuildServiceProvider();

        var testMe = CreateTestObject(provider);

        var video = new VideoInfo
                    {
                        Id = 1,
                        GroupId = 10,
                        OriginalCreator = new GroupShortInfo {Id = 11},
                        TaggedGroups = [],
                        Mentions =
                        [
                            new TaggedGroup {GroupId = 1001, GroupNickname = "mention-1001"}
                        ],
                        Description = "Hey throw back to @1001"
                    };

        // Act
        await testMe.SetExtraVideoInfo([video]);

        // Assert
        video.Kpi.Should().NotBeNull();
        video.Kpi.VideoId.Should().Be(video.Id);
        video.Kpi.Comments.Should().Be(video.Id); // Ensure selected correct KPI

        video.Owner.Should().NotBeNull();
        video.Owner.Id.Should().Be(video.GroupId);
        video.OriginalCreator.Id.Should().Be(11);
        video.OriginalCreator.Nickname.Should().Be("Group-11"); // Ensure we got value from mock

        video.ThumbnailUrl.Should().NotBeNullOrWhiteSpace();
        video.RedirectUrl.Should().NotBeNullOrWhiteSpace();
        video.SignedCookies.Should().NotBeNull();

        video.Description.Should().Be("Hey throw back to @1001");
    }

    [Fact(DisplayName = "👍👍👍 Setting liked by current user works")]
    public async Task EnsureVideoLikedFlagWorks()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTestVideoServices(testOut);
        await using var provider = services.BuildServiceProvider();

        var video1 = new VideoInfo
                     {
                         Id = 1,
                         GroupId = 10,
                         OriginalCreator = new GroupShortInfo {Id = 11},
                         TaggedGroups = [],
                         Mentions = new List<TaggedGroup>()
                     };
        var video2 = new VideoInfo
                     {
                         Id = 2,
                         GroupId = 20,
                         OriginalCreator = new GroupShortInfo {Id = 22},
                         TaggedGroups = [],
                         Mentions = new List<TaggedGroup>()
                     };


        var repo = new Mock<IVideoExtraDataRepository>(MockBehavior.Strict);
        repo.Setup(r => r.GetLikedVideoIds(It.IsAny<long>(), It.IsAny<long[]>())).Returns(Task.FromResult(new[] {video2.Id}));
        repo.Setup(s => s.GetFollowGroupIds(It.IsAny<long>(), It.IsAny<IEnumerable<long>>(), It.IsAny<DateTime>()))
            .Returns(Task.FromResult(new Dictionary<long, int>()));

        var testMe = CreateTestObject(provider, repo: repo);

        // Act
        await testMe.SetExtraVideoInfo([video1, video2]);

        // Assert
        video1.LikedByCurrentUser.Should().BeFalse();
        video2.LikedByCurrentUser.Should().BeTrue();
    }

    [Fact(DisplayName = "👍👍👍 Setting IsFollowRecommended works")]
    public async Task EnsureSettingIsFollowRecommendedWorks()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTestVideoServices(testOut);
        await using var provider = services.BuildServiceProvider();

        var video1 = new VideoInfo
                     {
                         Id = 1,
                         GroupId = 10,
                         OriginalCreator = new GroupShortInfo {Id = 11},
                         TaggedGroups = [],
                         Mentions = []
                     };

        var repo = new Mock<IVideoExtraDataRepository>(MockBehavior.Strict);
        repo.Setup(r => r.GetLikedVideoIds(It.IsAny<long>(), It.IsAny<long[]>())).Returns(Task.FromResult(Array.Empty<long>()));
        repo.Setup(s => s.GetFollowGroupIds(It.IsAny<long>(), It.IsAny<IEnumerable<long>>(), It.IsAny<DateTime>()))
            .Returns(Task.FromResult(new Dictionary<long, int> {{video1.GroupId, 3}}));

        var testMe = CreateTestObject(provider, repo: repo);

        // Act
        await testMe.SetExtraVideoInfo([video1]);

        // Assert
        video1.IsFollowRecommended.Should().BeTrue();
    }

    [Fact(DisplayName = "👍👍👍 Setting follow relations works")]
    public async Task EnsureVideoFollowRelationWorks()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTestVideoServices(testOut);
        await using var provider = services.BuildServiceProvider();

        var video1 = new VideoInfo
                     {
                         Id = 1,
                         GroupId = 100,
                         OriginalCreator = new GroupShortInfo {Id = 11},
                         TaggedGroups = [],
                         Mentions = new List<TaggedGroup>()
                     };
        var video2 = new VideoInfo
                     {
                         Id = 2,
                         GroupId = 200,
                         OriginalCreator = new GroupShortInfo {Id = 22},
                         TaggedGroups = [],
                         Mentions = new List<TaggedGroup>()
                     };

        var followRelationSet = new Dictionary<long, FollowRelationInfo>
                                {
                                    {100, new FollowRelationInfo {GroupId = 100, IsFollowed = true, IsFollower = true}}
                                };

        var followRelations = new Mock<IFollowRelationService>();
        followRelations.Setup(s => s.GetFollowRelations(It.IsAny<long>(), It.IsAny<ISet<long>>()))
                       .Returns(Task.FromResult(followRelationSet));

        var testMe = CreateTestObject(provider, followRelations);

        // Act
        await testMe.SetExtraVideoInfo([video1, video2]);

        // Assert
        video1.IsFollowed.Should().BeTrue();
        video1.IsFollower.Should().BeTrue();
        video1.IsFriend.Should().BeTrue();

        video2.IsFollowed.Should().BeFalse();
        video2.IsFollower.Should().BeFalse();
        video2.IsFriend.Should().BeFalse();

        var currentUser = provider.GetRequiredService<UserInfo>();

        followRelations.Verify(
            s => s.GetFollowRelations(
                currentUser.UserMainGroupId,
                It.Is(
                    new HashSet<long>
                    {
                        100,
                        200,
                        11,
                        22
                    },
                    new HashSetEqualityComparer<long>()
                )
            ),
            Times.Once
        );
    }

    [Fact(DisplayName = "👍👍👍 Blocked tagged groups removed")]
    public async Task EnsureBlockedTaggedGroupsRemoved()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTestVideoServices(testOut);
        await using var provider = services.BuildServiceProvider();

        var video = new VideoInfo
                    {
                        Id = 1,
                        GroupId = 100,
                        OriginalCreator = new GroupShortInfo {Id = 11},
                        TaggedGroups =
                        [
                            new TaggedGroup {GroupId = 201, GroupNickname = "blocked"},
                            new TaggedGroup {GroupId = 202, GroupNickname = "not blocked"}
                        ],
                        Mentions = []
                    };

        var currentUser = provider.GetRequiredService<UserInfo>();
        var socialSharedService = provider.GetRequiredService<Mock<ISocialSharedService>>();
        socialSharedService.Setup(s => s.GetBlocked(currentUser.UserMainGroupId, It.IsAny<long[]>()))
                           .Returns(Task.FromResult(new[] {201l}));

        var testMe = CreateTestObject(provider);

        // Act
        await testMe.SetExtraVideoInfo([video]);

        // Assert
        video.TaggedGroups.Length.Should().Be(1);
        video.TaggedGroups[0].Should().BeEquivalentTo(new TaggedGroup {GroupId = 202, GroupNickname = "not blocked"});

        socialSharedService.Verify(
            s => s.GetBlocked(currentUser.UserMainGroupId, It.Is(new long[] {201, 202}, new HashSetEqualityComparer<long>())),
            Times.Once
        );
    }

    [Fact(DisplayName = "👍👍👍 Blocked groups removed from mentions")]
    public async Task EnsureBlockedGroupsRemovedFromMentions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTestVideoServices(testOut);
        await using var provider = services.BuildServiceProvider();

        var video = new VideoInfo
                    {
                        Id = 1,
                        GroupId = 100,
                        OriginalCreator = new GroupShortInfo {Id = 11},
                        TaggedGroups = [],
                        Mentions =
                        [
                            new TaggedGroup {GroupId = 201, GroupNickname = "blocked"},
                            new TaggedGroup {GroupId = 202, GroupNickname = "not blocked"}
                        ],
                        Description = "Hey throw back to @201 @202"
                    };

        var currentUser = provider.GetRequiredService<UserInfo>();
        var socialSharedService = provider.GetRequiredService<Mock<ISocialSharedService>>();
        socialSharedService.Setup(s => s.GetBlocked(currentUser.UserMainGroupId, It.IsAny<long[]>()))
                           .Returns(Task.FromResult(new[] {201l}));

        var testMe = CreateTestObject(provider);

        // Act
        await testMe.SetExtraVideoInfo([video]);

        // Assert
        video.Mentions.Count.Should().Be(1);
        video.Mentions[0].Should().BeEquivalentTo(new TaggedGroup {GroupId = 202, GroupNickname = "not blocked"});
        video.Description.Should().Be("Hey throw back to  @202");

        socialSharedService.Verify(
            s => s.GetBlocked(currentUser.UserMainGroupId, It.Is(new long[] {201, 202}, new HashSetEqualityComparer<long>())),
            Times.Once
        );
    }

    private IVideoExtraDataProvider CreateTestObject(
        IServiceProvider provider,
        Mock<IFollowRelationService> followRelations = null,
        Mock<IVideoKpiCachingService> kpiService = null,
        Mock<IVideoExtraDataRepository> repo = null
    )
    {
        if (followRelations == null)
        {
            followRelations = new Mock<IFollowRelationService>(MockBehavior.Strict);
            followRelations.Setup(s => s.GetFollowRelations(It.IsAny<long>(), It.IsAny<ISet<long>>()))
                           .Returns(Task.FromResult(new Dictionary<long, FollowRelationInfo>()));
        }

        if (kpiService == null)
        {
            kpiService = new Mock<IVideoKpiCachingService>();
            kpiService.Setup(s => s.GetVideosKpis(It.IsAny<long[]>()))
                      .Returns(
                           (long[] videoIds) =>
                           {
                               var result = new Dictionary<long, VideoKpi>();

                               foreach (var id in videoIds)
                                   result[id] = new VideoKpi
                                                {
                                                    Comments = id,
                                                    Likes = id,
                                                    Remixes = id,
                                                    Shares = id,
                                                    Views = id,
                                                    BattlesLost = id,
                                                    BattlesWon = id,
                                                    VideoId = id
                                                };

                               return Task.FromResult(result);
                           }
                       );
        }

        if (repo == null)
        {
            repo = new Mock<IVideoExtraDataRepository>();
            repo.Setup(s => s.GetFollowGroupIds(It.IsAny<long>(), It.IsAny<IEnumerable<long>>(), It.IsAny<DateTime>()))
                .Returns(Task.FromResult(new Dictionary<long, int>()));
        }

        var extraDataProvider = new VideoExtraDataProvider(
            followRelations.Object,
            provider.GetRequiredService<UserInfo>(),
            kpiService.Object,
            provider.GetRequiredService<ISocialSharedService>(),
            repo.Object,
            provider.GetRequiredService<IVideoAssetUrlGenerator>(),
            provider.GetRequiredService<VideoNamingHelper>()
        );

        return extraDataProvider;
    }
}