using System;
using System.Linq;
using Frever.ClientService.Contract.Social;
using Frever.Shared.MainDb;
using Frever.Shared.MainDb.Entities;
using Frever.Video.Contract;

namespace Frever.Video.Core.Features.VideoInformation.DataAccess;

public interface IVideoInfoRepository
{
    IQueryable<VideoInfo> GetVideoInfo(FetchVideoInfoFrom fetchFrom, long[] videoIds);
}

public class PersistentVideoInfoRepository(IReadDb readDb, IWriteDb writeDb) : IVideoInfoRepository
{
    public IQueryable<VideoInfo> GetVideoInfo(FetchVideoInfoFrom fetchFrom, long[] videoIds)
    {
        var videos = GetVideos(fetchFrom, videoIds);
        return GetVideoInfo(videos);
    }

    private IQueryable<Frever.Shared.MainDb.Entities.Video> GetVideos(FetchVideoInfoFrom fetchFrom, long[] videoIds)
    {
        var queryable = fetchFrom == FetchVideoInfoFrom.ReadDb ? readDb.Video : writeDb.Video;

        var videoAccess = Enum.GetValues(typeof(VideoAccess)).Cast<VideoAccess>();

        return queryable.Where(v => videoAccess.Contains(v.Access))
                        .Where(v => !v.IsDeleted)
                        .Where(v => v.Group.DeletedAt == null && !v.Group.IsBlocked)
                        .Where(v => videoIds.Contains(v.Id));
    }

    private IQueryable<VideoInfo> GetVideoInfo(IQueryable<Frever.Shared.MainDb.Entities.Video> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return source.Select(
            v => new VideoInfo
                 {
                     Size = v.Size,
                     CreatedTime = v.CreatedTime,
                     Duration = v.Duration,
                     GroupId = v.GroupId,
                     Links = v.Links,
                     RemixedFromVideoId = v.RemixedFromVideoId,
                     CharactersCount = v.CharactersCount,
                     Id = v.Id,
                     TopListPosition = v.ToplistPosition ?? long.MaxValue,
                     IsRemixable = v.IsRemixable,
                     IsDeleted = v.IsDeleted,
                     Access = v.Access,
                     Version = v.Version,
                     Kpi = new VideoKpi {VideoId = v.Id},
                     Description = v.Description,
                     OriginalCreator =
                         v.RemixedFromVideo == null
                             ? null
                             : new GroupShortInfo {Id = v.RemixedFromVideo.GroupId},
                     TaggedGroups =
                         v.VideoGroupTags.Where(e => e.IsCharacterTag)
                          .Select(e => new TaggedGroup {GroupId = e.Group.Id, GroupNickname = e.Group.NickName})
                          .ToArray(),
                     NonCharacterTaggedGroups =
                         v.VideoGroupTags.Where(e => !e.IsCharacterTag)
                          .Select(e => new TaggedGroup {GroupId = e.Group.Id, GroupNickname = e.Group.NickName})
                          .ToArray(),
                     Mentions =
                         v.VideoMentions.Where(e => !e.Group.IsBlocked)
                          .Select(e => new TaggedGroup {GroupId = e.Group.Id, GroupNickname = e.Group.NickName})
                          .ToList(),
                     Hashtags =
                         v.VideoAndHashtag.Where(e => !e.Hashtag.IsDeleted)
                          .Select(
                               e => new HashtagInfo
                                    {
                                        Id = e.Hashtag.Id,
                                        Name = e.Hashtag.Name,
                                        ViewsCount = e.Hashtag.ViewsCount,
                                        UsageCount = e.Hashtag.VideoCount
                                    }
                           )
                          .ToList(),
                     Songs = v.SongInfo,
                     UserSounds = v.UserSoundInfo,
                     AllowRemix = v.AllowRemix,
                     AllowComment = v.AllowComment,
                     Location = v.Location != null ? v.Location.ToText() : null,
                     AiContentId = v.AiContentId,
                     Key = v.Id.ToString()
                 }
        );
    }
}