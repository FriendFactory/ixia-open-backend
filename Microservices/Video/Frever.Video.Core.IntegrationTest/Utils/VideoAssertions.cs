using FluentAssertions;
using Frever.Common.IntegrationTesting;
using Frever.Shared.MainDb.Entities;
using Frever.Video.Contract;
using Microsoft.EntityFrameworkCore;

namespace Frever.Video.Core.IntegrationTest.Utils;

public static class VideoAssertions
{
    /// <summary>
    ///     Checks if data in video info corresponds to data in database.
    /// </summary>
    public static async Task AssertVideoInfo(this DataEnvironment dataEnvironment, IEnumerable<VideoInfo> videos)
    {
        ArgumentNullException.ThrowIfNull(dataEnvironment);
        ArgumentNullException.ThrowIfNull(videos);

        var ids = videos.Select(v => v.Id).ToArray();
        var groupIds = videos.Select(v => v.GroupId).ToArray();

        var expectedVideos = await dataEnvironment.Db.Video.Where(v => ids.Contains(v.Id))
                                                  .Include(v => v.VideoMentions)
                                                  .Include(v => v.VideoAndHashtag)
                                                  .ThenInclude(vh => vh.Hashtag)
                                                  .ToArrayAsync();
        var expectedKpis = await dataEnvironment.Db.VideoKpi.Where(v => ids.Contains(v.VideoId)).ToArrayAsync();
        var expectedGroupInfo = await dataEnvironment.Db.Group.Where(g => groupIds.Contains(g.Id)).Include(g => g.User).ToArrayAsync();

        foreach (var actual in videos)
        {
            var expectedVideo = expectedVideos.Single(v => v.Id == actual.Id);
            var expectedKpi = expectedKpis.Single(k => k.VideoId == actual.Id);
            var expectedOwnerGroup = expectedGroupInfo.Single(g => g.Id == actual.Owner.Id);

            await AssertMainVideoInfo(actual, expectedVideo);
            await AssertVideoKpi(actual, expectedKpi);
            await AssertVideoOwner(actual, expectedOwnerGroup);
        }
    }

    private static async Task AssertVideoOwner(VideoInfo actual, Group expected)
    {
        actual.Owner.Id.Should().Be(expected.Id);
        actual.Owner.Nickname.Should().Be(expected.NickName);
    }

    private static async Task AssertVideoKpi(VideoInfo actual, VideoKpi expected)
    {
        actual.Kpi.Comments.Should().Be(expected.Comments);
        actual.Kpi.Likes.Should().Be(expected.Likes);
        actual.Kpi.Remixes.Should().Be(expected.Remixes);
        actual.Kpi.Shares.Should().Be(expected.Shares);
        actual.Kpi.Views.Should().Be(expected.Views);
        actual.Kpi.BattlesLost.Should().Be(expected.BattlesLost);
        actual.Kpi.BattlesWon.Should().Be(expected.BattlesWon);
    }

    private static Task AssertMainVideoInfo(VideoInfo actual, Shared.MainDb.Entities.Video expected)
    {
        actual.Access.Should().Be(expected.Access);
        actual.Description.Should().Be(expected.Description);
        actual.Duration.Should().Be(expected.Duration);
        actual.Owner.Id.Should().Be(expected.GroupId);
        actual.Size.Should().Be(expected.Size);
        actual.Songs.Should().BeEquivalentTo(expected.SongInfo);
        actual.Version.Should().Be(expected.Version);
        actual.AllowComment.Should().Be(expected.AllowComment);
        actual.AllowRemix.Should().Be(expected.AllowRemix);
        actual.CharactersCount.Should().Be(expected.CharactersCount);
        actual.GroupId.Should().Be(expected.GroupId);
        actual.IsDeleted.Should().Be(expected.IsDeleted);
        actual.IsRemixable.Should().Be(expected.IsRemixable);
        actual.RemixedFromVideoId.Should().Be(expected.RemixedFromVideoId);

        actual.ThumbnailUrl.Should().NotBeNullOrEmpty();
        Uri.IsWellFormedUriString(actual.ThumbnailUrl, UriKind.Absolute).Should().BeTrue();
        actual.RedirectUrl.Should().NotBeNullOrEmpty();
        Uri.IsWellFormedUriString(actual.RedirectUrl, UriKind.Absolute).Should().BeTrue();

        actual.SignedCookies.Should().NotBeNull();
        actual.SignedCookies.Count.Should().BePositive();

        actual.Mentions.Select(m => m.GroupId).Order().Should().BeEquivalentTo(expected.VideoMentions.Select(m => m.GroupId).Order());


        actual.Hashtags.Select(
                   h => new
                        {
                            h.Id,
                            h.Name,
                            h.UsageCount,
                            h.ViewsCount
                        }
               )
              .OrderBy(v => v.Id)
              .Should()
              .BeEquivalentTo(
                   expected.VideoAndHashtag.Select(vh => vh.Hashtag)
                           .Select(
                                h => new
                                     {
                                         h.Id,
                                         h.Name,
                                         h.ViewsCount,
                                         UsageCount = h.VideoCount
                                     }
                            )
                           .OrderBy(a => a.Id)
               );

        return Task.CompletedTask;
    }
}