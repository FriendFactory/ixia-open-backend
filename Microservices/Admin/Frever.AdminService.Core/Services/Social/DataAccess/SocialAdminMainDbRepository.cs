using System;
using System.Linq;
using System.Threading.Tasks;
using Frever.AdminService.Core.Services.Social.Contracts;
using Frever.Shared.MainDb;
using Frever.Shared.MainDb.Entities;
using Microsoft.EntityFrameworkCore;

namespace Frever.AdminService.Core.Services.Social.DataAccess;

public sealed class SocialAdminMainDbRepository(IWriteDb db) : ISocialAdminMainDbRepository
{
    public IQueryable<long> GetGroupIdsByFollowers(DateTime startDate, DateTime endDate)
    {
        return db.Follower.Where(f => f.Time >= startDate && endDate >= f.Time)
                 .GroupBy(e => e.FollowingId)
                 .OrderByDescending(e => e.Count())
                 .Select(e => e.Key);
    }

    public IQueryable<long> GetGroupIdsByTotalVideos(DateTime startDate, DateTime endDate)
    {
        return db.Video.Where(v => v.CreatedTime >= startDate && endDate >= v.CreatedTime)
                 .GroupBy(e => e.GroupId)
                 .OrderByDescending(e => e.Count())
                 .Select(e => e.Key);
    }

    public IQueryable<long> GetGroupIdsByLikes(DateTime startDate, DateTime endDate)
    {
        return db.Like.Where(l => l.Time >= startDate && endDate >= l.Time)
                 .Join(db.Video, l => l.VideoId, v => v.Id, (l, v) => new {v.GroupId})
                 .GroupBy(e => e.GroupId)
                 .OrderByDescending(v => v.Count())
                 .Select(e => e.Key);
    }

    public async Task<ProfileKpiDto> GetProfileKpis(long groupId)
    {
        var kpis = await db.Group.Where(e => e.Id == groupId)
                           .Select(
                                e => new ProfileKpiDto
                                     {
                                         FollowersCount = e.TotalFollowers, VideoLikesCount = e.TotalLikes, TotalVideoCount = e.TotalVideos
                                     }
                            )
                           .FirstOrDefaultAsync();

        kpis.FollowingCount = await db.Follower.CountAsync(f => f.FollowerId == groupId);
        kpis.UserSoundsCount = await db.UserSound.CountAsync(e => e.GroupId == groupId);
        kpis.TaggedInVideoCount = await db.VideoGroupTag.CountAsync(e => e.GroupId == groupId);
        kpis.PublishedVideoCount = await db.Video.CountAsync(v => !v.IsDeleted && v.GroupId == groupId && v.Access != VideoAccess.Private);

        var userBalance = await db.AssetStoreTransactions.Where(e => e.GroupId == groupId)
                                  .GroupBy(e => true)
                                  .Select(
                                       t => new
                                            {
                                                SoftCurrencyAmount = t.Sum(e => e.SoftCurrencyAmount),
                                                HardCurrencyAmount = t.Sum(e => e.HardCurrencyAmount)
                                            }
                                   )
                                  .SingleOrDefaultAsync();

        kpis.SoftCurrency = userBalance?.SoftCurrencyAmount ?? 0;
        kpis.HardCurrency = userBalance?.HardCurrencyAmount ?? 0;

        return kpis;
    }

    public Task<long[]> GetFeaturedGroupIds()
    {
        return db.User.Where(u => u.IsFeatured).Select(u => u.MainGroupId).ToArrayAsync();
    }

    public IQueryable<User> GetUsers()
    {
        return db.User;
    }

    public IQueryable<UserActivity> GetUserActivity()
    {
        return db.UserActivities;
    }
}