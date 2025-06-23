using FluentAssertions;
using Frever.Common.IntegrationTesting;
using Frever.Common.IntegrationTesting.Data;
using Frever.Common.IntegrationTesting.Data.Video;
using Frever.Common.Testing;
using Frever.Shared.MainDb.Entities;
using Frever.Video.Core.Features.Comments;
using Frever.Video.Core.Features.Feeds.Trending;
using Frever.Video.Core.IntegrationTest.Utils;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Frever.Video.Core.IntegrationTest.Features;

public class CommentsIntegrationTest(ITestOutputHelper testOut) : IntegrationTestBase
{
    [Fact]
    public async Task CommentsAdding_HappyPath()
    {
        var services = new ServiceCollection();
        services.AddVideoIntegrationTests(testOut);

        await using var provider = services.BuildServiceProvider();

        var dataEnv = provider.GetRequiredService<DataEnvironment>();

        var user = await dataEnv.WithUserAndGroup(new UserAndGroupCreateParams {CountryIso3 = "swe", LanguageIso3 = "swe"});
        var secondUser = await dataEnv.WithUserAndGroup(new UserAndGroupCreateParams {CountryIso3 = "swe", LanguageIso3 = "swe"});

        provider.SetCurrentUser(user);

        await dataEnv.WithTwoGeoClusters();

        var videoAuthors = await dataEnv.WithUsersAndGroups(
                               Enumerable.Range(0, 50)
                                         .Select(
                                              id => new UserAndGroupCreateParams
                                                    {
                                                        NickName = $"video-author-{id}-{Guid.NewGuid():N}",
                                                        CountryIso3 = id < 20 ? "swe" : "usa",
                                                        LanguageIso3 = id < 20 ? "swe" : "eng"
                                                    }
                                          )
                                         .ToArray()
                           );

        var videoInput = Enumerable.Range(0, 50)
                                   .Select(
                                        (id, index) => new VideoInput
                                                       {
                                                           LevelId = id,
                                                           GroupId = videoAuthors[index].MainGroupId,
                                                           Access = VideoAccess.Public,
                                                           ToplistPosition = index + 1,
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

        await dataEnv.WithVideo(videoInput);
        var trendingVideoFeed = provider.GetRequiredService<ITrendingVideoFeed>();

        var videos = await trendingVideoFeed.GetTrendingVideos(null, 20);

        videos.Should().HaveCount(20);

        var videoId = videos.Skip(3).First().Id;

        var commentReadingService = provider.GetRequiredService<ICommentReadingService>();
        var commentModificationService = provider.GetRequiredService<ICommentModificationService>();

        var topLevelComment = await commentModificationService.AddComment(videoId, new AddCommentRequest {Text = "Hi!"});
        var reply = await commentModificationService.AddComment(
                        videoId,
                        new AddCommentRequest {Text = "Hello", ReplyToCommentId = topLevelComment.Id}
                    );

        await commentModificationService.LikeComment(videoId, reply.Id);

        using var scope = provider.CreateScope();
        scope.ServiceProvider.SetCurrentUser(secondUser);
        var commentModificationServiceForSecondUser = scope.ServiceProvider.GetRequiredService<ICommentModificationService>();
        await commentModificationServiceForSecondUser.LikeComment(videoId, reply.Id);


        var existingComments = await commentReadingService.GetRootComments(videoId, takeOlder: 20);
        var addedTopLevel = existingComments.FirstOrDefault(c => c.Id == topLevelComment.Id);

        addedTopLevel.Should().NotBeNull();
        addedTopLevel.VideoId.Should().Be(videoId);
        addedTopLevel.Text.Should().Be(topLevelComment.Text);
        addedTopLevel.GroupId.Should().Be(user.MainGroupId);
        addedTopLevel.ReplyCount.Should().Be(1);
        addedTopLevel.LikeCount.Should().Be(0);


        var existingThread = await commentReadingService.GetThreadComments(videoId, addedTopLevel.Key, takeOlder: 20);
        var addedReply = existingThread.FirstOrDefault(c => c.Id == reply.Id);

        addedReply.VideoId.Should().Be(videoId);
        addedReply.Text.Should().Be(reply.Text);
        addedReply.GroupId.Should().Be(user.MainGroupId);
        addedReply.ReplyCount.Should().Be(0);
        addedReply.LikeCount.Should().Be(2);
        addedReply.ReplyToComment.Should().NotBeNull();
        addedReply.ReplyToComment.CommentId.Should().Be(addedTopLevel.Id);
        addedReply.ReplyToComment.GroupId.Should().Be(user.MainGroupId);
    }

    [Fact]
    public async Task CommentsPinning_HappyPath()
    {
        var services = new ServiceCollection();
        services.AddVideoIntegrationTests(testOut);

        await using var provider = services.BuildServiceProvider();

        var dataEnv = provider.GetRequiredService<DataEnvironment>();

        var user = await dataEnv.WithUserAndGroup(new UserAndGroupCreateParams {CountryIso3 = "swe", LanguageIso3 = "swe"});
        var secondUser = await dataEnv.WithUserAndGroup(new UserAndGroupCreateParams {CountryIso3 = "swe", LanguageIso3 = "swe"});

        provider.SetCurrentUser(user);

        await dataEnv.WithTwoGeoClusters();
        var videoInput = Enumerable.Range(0, 50)
                                   .Select(
                                        (id, index) => new VideoInput
                                                       {
                                                           LevelId = id,
                                                           GroupId = user.MainGroupId,
                                                           Access = VideoAccess.Public,
                                                           ToplistPosition = index + 1,
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
                                                                 },
                                                           AllowComment = true
                                                       }
                                    )
                                   .ToArray();

        await dataEnv.WithVideo(videoInput);
        var trendingVideoFeed = provider.GetRequiredService<ITrendingVideoFeed>();

        var videos = await trendingVideoFeed.GetTrendingVideos(null, 20);

        videos.Should().HaveCount(20);

        var videoId = videos.Skip(3).First().Id;

        var commentReadingService = provider.GetRequiredService<ICommentReadingService>();
        var commentModificationService = provider.GetRequiredService<ICommentModificationService>();

        var topLevelComment1 = await commentModificationService.AddComment(videoId, new AddCommentRequest {Text = "Hi!"});
        var topLevelComment2 = await commentModificationService.AddComment(videoId, new AddCommentRequest {Text = "Hi!!"});
        var topLevelComment3 = await commentModificationService.AddComment(videoId, new AddCommentRequest {Text = "Hi!!!"});

        var notPinnedComment = await commentReadingService.GetRootComments(videoId, takeOlder: 20);
        notPinnedComment.Should().HaveCount(3);

        notPinnedComment[0].Id.Should().Be(topLevelComment3.Id);
        notPinnedComment[1].Id.Should().Be(topLevelComment2.Id);
        notPinnedComment[2].Id.Should().Be(topLevelComment1.Id);

        await commentModificationService.PinComment(videoId, topLevelComment2.Id);


        using var scope = provider.CreateScope();
        scope.ServiceProvider.SetCurrentUser(secondUser);
        var secondCommentReading = scope.ServiceProvider.GetRequiredService<ICommentReadingService>();

        var pinnedComments = await secondCommentReading.GetPinnedComments(videoId);
        pinnedComments.Should().HaveCount(1);
        pinnedComments[0].Id.Should().Be(topLevelComment2.Id);
    }
}