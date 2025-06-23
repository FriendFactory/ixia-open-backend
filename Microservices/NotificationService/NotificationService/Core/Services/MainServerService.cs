using System.Threading.Tasks;
using Frever.Client.Shared.Files;
using Frever.ClientService.Contract.Ai;
using Frever.Shared.MainDb.Entities;
using Microsoft.EntityFrameworkCore;
using NotificationService.DataAccess;

namespace NotificationService.Core;

public interface IMainServerService
{
    Task<bool> IsMyFriend(long currentGroupId, long groupId);

    Task<bool> HaveFollower(long groupId, long followerId);

    Task<long[]> GetFriendIds(long currentGroupId);

    Task<AiGeneratedContentShortInfo> GetContentInfo(long contentId, long groupId);
}

internal sealed class MainServerService(INotificationRepository repo, IFileStorageService fileStorage) : IMainServerService
{
    public async Task<bool> IsMyFriend(long currentGroupId, long groupId)
    {
        return await repo.AreFriends(groupId, currentGroupId);
    }

    public async Task<bool> HaveFollower(long groupId, long followerId)
    {
        return await repo.HaveFollower(groupId, followerId);
    }

    public Task<long[]> GetFriendIds(long currentGroupId)
    {
        return repo.GetFriendGroupIds(currentGroupId).ToArrayAsync();
    }

    public async Task<AiGeneratedContentShortInfo> GetContentInfo(long contentId, long groupId)
    {
        var content = await repo.GetAiContent(contentId, groupId);
        if (content == null)
            return null;

        if (content.Type == AiGeneratedContentType.Image)
            await fileStorage.InitUrls<AiGeneratedImage>(content);
        else
            await fileStorage.InitUrls<AiGeneratedVideo>(content);

        return new AiGeneratedContentShortInfo
               {
                   Id = content.ContentId,
                   Type = content.Type,
                   CreatedAt = content.CreatedAt,
                   RemixedFromAiGeneratedContentId = content.RemixedFromAiGeneratedContentId,
                   Files = content.Files,
                   Group = null
               };
    }
}

public class AiGeneratedImageFileConfig : DefaultFileMetadataConfiguration<AiGeneratedImage>
{
    public AiGeneratedImageFileConfig()
    {
        AddMainFile("jpeg");
        AddThumbnail(128, "jpeg");
    }
}

public class AiGeneratedVideoFileConfig : DefaultFileMetadataConfiguration<AiGeneratedVideo>
{
    public AiGeneratedVideoFileConfig()
    {
        AddMainFile("mp4");
    }
}