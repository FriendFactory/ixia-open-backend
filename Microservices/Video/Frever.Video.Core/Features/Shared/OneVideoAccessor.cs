using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Frever.Client.Shared.Social.Services;
using Frever.Shared.MainDb;
using Frever.Shared.MainDb.Entities;
using Frever.Video.Contract;
using Frever.Video.Core.Utils;
using Microsoft.EntityFrameworkCore;

namespace Frever.Video.Core.Features.Shared;

public interface IOneVideoAccessor
{
    Task<bool> IsVideoAccessibleTo(
        FetchVideoInfoFrom fetchFrom,
        long groupId,
        long videoId,
        Expression<Func<Frever.Shared.MainDb.Entities.Video, bool>> extraCondition = null
    );

    Task<VideoInfo> GetVideo(FetchVideoInfoFrom fetchFrom, long groupId, long videoId);
}

public class PersistentOneVideoAccessor(
    IWriteDb writeDb,
    IReadDb readDb,
    ISocialSharedService socialSharedService,
    IVideoLoader videoLoader
) : IOneVideoAccessor
{
    public async Task<bool> IsVideoAccessibleTo(
        FetchVideoInfoFrom fetchFrom,
        long groupId,
        long videoId,
        Expression<Func<Frever.Shared.MainDb.Entities.Video, bool>> extraCondition = null
    )
    {
        var video = await GetVideoQuery(fetchFrom, videoId, extraCondition).FirstOrDefaultAsyncSafe();

        return await IsAccessible(video, fetchFrom, groupId);
    }

    public async Task<VideoInfo> GetVideo(FetchVideoInfoFrom fetchFrom, long groupId, long videoId)
    {
        var video = await GetVideoQuery(fetchFrom, videoId).FirstOrDefaultAsyncSafe();

        if (!await IsAccessible(video, fetchFrom, groupId))
            return null;

        var allInfo = await videoLoader.LoadVideoPage(fetchFrom, video);

        return allInfo.FirstOrDefault();
    }

    private async Task<bool> IsAccessible(Frever.Shared.MainDb.Entities.Video video, FetchVideoInfoFrom fetchFrom, long groupId)
    {
        if (video == null)
            return false;

        if (video.GroupId == groupId)
            return true;

        if (await socialSharedService.IsBlocked(groupId, video.GroupId))
            return false;

        var videoGroupTag = fetchFrom == FetchVideoInfoFrom.WriteDb ? writeDb.VideoGroupTag : readDb.VideoGroupTag;

        return video.Access switch
               {
                   VideoAccess.Private => false,
                   VideoAccess.Public => true,
                   VideoAccess.ForFollowers => await socialSharedService.IsFollowed(groupId, video.GroupId),
                   VideoAccess.ForFriends => await socialSharedService.IsFriend(groupId, video.GroupId),
                   VideoAccess.ForTaggedGroups => await videoGroupTag.AnyAsync(t => t.VideoId == video.Id && t.GroupId == groupId),
                   _ => throw new ArgumentOutOfRangeException(nameof(video.Access))
               };
    }

    private IQueryable<Frever.Shared.MainDb.Entities.Video> GetVideoQuery(
        FetchVideoInfoFrom fetchFrom,
        long videoId,
        Expression<Func<Frever.Shared.MainDb.Entities.Video, bool>> extraCondition = null
    )
    {
        extraCondition ??= v => true;

        var videoAccess = Enum.GetValues(typeof(VideoAccess)).Cast<VideoAccess>();

        var src = fetchFrom == FetchVideoInfoFrom.ReadDb ? readDb.Video : writeDb.Video;

        var videos = src.Where(v => videoAccess.Contains(v.Access))
                        .Where(v => !v.IsDeleted)
                        .Where(v => v.Group.DeletedAt == null && !v.Group.IsBlocked)
                        .Where(extraCondition)
                        .Where(v => v.Id == videoId);

        return videos;
    }
}