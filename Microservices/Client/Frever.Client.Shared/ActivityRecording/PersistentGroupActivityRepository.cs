using System;
using System.Linq;
using System.Threading.Tasks;
using Frever.Shared.MainDb;
using Frever.Shared.MainDb.Entities;

namespace Frever.Client.Shared.ActivityRecording;

public class PersistentGroupActivityRepository(IWriteDb mainDb) : IGroupActivityRepository
{
    private readonly IWriteDb _mainDb = mainDb ?? throw new ArgumentNullException(nameof(mainDb));

    public IQueryable<UserActivity> GetLifetimeGroupActivity(long groupId)
    {
        return _mainDb.UserActivities.Where(a => a.GroupId == groupId);
    }

    public async Task<long> RecordActivity(UserActivity activity)
    {
        ArgumentNullException.ThrowIfNull(activity);

        await _mainDb.UserActivities.AddAsync(activity);
        await _mainDb.SaveChangesAsync();

        return activity.Id;
    }

    public IQueryable<Video> GetVideoById(long videoId)
    {
        return _mainDb.Video.Where(v => v.Id == videoId);
    }
}