using System.Threading.Tasks;
using AuthServer.Permissions.Services;
using AuthServerShared;
using Frever.Shared.MainDb;
using Frever.Video.Contract;
using Microsoft.EntityFrameworkCore;

namespace Frever.Video.Core.Features.Feeds.AiContent;

public interface IAiContentVideoFeed
{
    Task<VideoInfo[]> GetAiContentVideo(long aiContentId);
}

public class AiContentVideoFeed(UserInfo currentUser, IUserPermissionService userPermissionService, IVideoLoader videoLoader, IWriteDb db)
    : IAiContentVideoFeed
{
    public async Task<VideoInfo[]> GetAiContentVideo(long aiContentId)
    {
        var aiContent = await db.AiGeneratedContent.FirstOrDefaultAsync(c => c.Id == aiContentId);
        if (aiContent == null)
            return [];

        var video = await db.GetGroupAvailableVideoQuery(aiContent.GroupId, currentUser)
                            .Result.FirstOrDefaultAsync(v => v.AiContentId == aiContentId);

        if (video == null)
            return [];

        return await videoLoader.LoadVideoPage(FetchVideoInfoFrom.WriteDb, video);
    }
}