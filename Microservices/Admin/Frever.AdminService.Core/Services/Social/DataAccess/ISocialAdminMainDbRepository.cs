using System;
using System.Linq;
using System.Threading.Tasks;
using Frever.AdminService.Core.Services.Social.Contracts;
using Frever.Shared.MainDb.Entities;

namespace Frever.AdminService.Core.Services.Social.DataAccess;

public interface ISocialAdminMainDbRepository
{
    Task<long[]> GetFeaturedGroupIds();

    IQueryable<long> GetGroupIdsByFollowers(DateTime startDate, DateTime endDate);

    IQueryable<long> GetGroupIdsByTotalVideos(DateTime startDate, DateTime endDate);

    IQueryable<long> GetGroupIdsByLikes(DateTime startDate, DateTime endDate);

    Task<ProfileKpiDto> GetProfileKpis(long groupId);

    IQueryable<User> GetUsers();

    IQueryable<UserActivity> GetUserActivity();
}