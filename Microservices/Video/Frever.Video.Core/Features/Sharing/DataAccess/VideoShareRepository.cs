using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Frever.Shared.MainDb;
using Frever.Shared.MainDb.Entities;
using Microsoft.EntityFrameworkCore;

namespace Frever.Video.Core.Features.Sharing.DataAccess;

public interface IVideoShareRepository
{
    Task<int> GetGroupFollowerCount(long groupId);
    Task<int> GetVideoShareCount(long groupId, DateTime fromDate);
    Task<bool> IsVideoAvailable(long videoId);
    Task<Dictionary<long, string>> GetSongLabels(IEnumerable<long> songIds);
}

public class PersistentVideoShareRepository(IWriteDb db) : IVideoShareRepository
{
    public Task<int> GetGroupFollowerCount(long groupId)
    {
        return db.Group.Where(g => g.Id == groupId).Select(g => g.TotalFollowers).FirstOrDefaultAsync();
    }

    public Task<int> GetVideoShareCount(long groupId, DateTime fromDate)
    {
        return db.UserActivities
                 .Where(e => e.GroupId == groupId && e.ActionType == UserActionType.PublishedVideoShare && e.OccurredAt >= fromDate)
                 .CountAsync();
    }

    public Task<bool> IsVideoAvailable(long videoId)
    {
        return db.Video.Where(v => v.Id == videoId && v.Access != VideoAccess.Private && !v.IsDeleted)
                 .Where(v => !v.Group.IsBlocked && v.Group.DeletedAt == null)
                 .Where(v => !db.VideoReport.Any(r => r.HideVideo && r.VideoId == v.Id))
                 .AnyAsync();
    }

    public async Task<Dictionary<long, string>> GetSongLabels(IEnumerable<long> songIds)
    {
        var all = await db.Song.Where(e => songIds.Contains(e.Id)).Select(e => new {e.Id, e.Label.Name}).ToArrayAsync();

        var result = new Dictionary<long, string>();

        foreach (var item in all)
            result[item.Id] = item.Name;

        return result;
    }
}