using System.Linq;
using System.Threading.Tasks;
using Frever.Shared.MainDb.Entities;

namespace Frever.AdminService.Core.Services.VideoModeration.DataAccess;

public interface IVideoRepository
{
    IQueryable<Shared.MainDb.Entities.Video> GetVideos();

    Task PublishVideo(long videoId);

    Task UnPublishVideo(long videoId);

    Task SetVideoDeleted(long videoId, long groupId, bool isDeleted);

    Task MarkAccountVideosAsDeleted(long groupId);

    Task EraseAccountComments(long groupId);

    IQueryable<VideoKpi> GetVideoKpi();

    IQueryable<Comment> GetComments();

    Task SetCommentDeleted(long videoId, long commentId, bool isDeleted);

    IQueryable<Group> GetGroups();

    IQueryable<Shared.MainDb.Entities.Video> GetVideosByHashtagId(long hashtagId);

    Task SaveChanges();
}