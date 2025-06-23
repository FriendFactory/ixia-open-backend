using System.Linq;
using System.Threading.Tasks;
using Frever.Shared.MainDb.Entities;

namespace Frever.Client.Shared.ActivityRecording;

public interface IGroupActivityRepository
{
    IQueryable<UserActivity> GetLifetimeGroupActivity(long groupId);

    Task<long> RecordActivity(UserActivity activity);

    IQueryable<Video> GetVideoById(long videoId);
}