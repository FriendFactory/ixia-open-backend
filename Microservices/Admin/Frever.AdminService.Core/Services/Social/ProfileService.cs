using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AuthServer.Permissions.Services;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Common.Infrastructure.Utils;
using Frever.AdminService.Core.Services.Social.Contracts;
using Frever.AdminService.Core.Services.Social.DataAccess;
using Frever.AdminService.Core.Utils;
using Frever.Client.Shared.Files;
using Frever.Shared.MainDb.Entities;
using Microsoft.AspNet.OData.Query;
using Microsoft.EntityFrameworkCore;
using ValidationException = FluentValidation.ValidationException;

namespace Frever.AdminService.Core.Services.Social;

internal sealed class ProfileService(
    IMapper mapper,
    IFileStorageService fileStorage,
    ISocialAdminMainDbRepository mainDbRepo,
    IUserPermissionService permissionService)
    : IProfileService
{
    public async Task<ResultWithCount<ProfileDto>> GetProfiles(ODataQueryOptions<ProfileDto> options)
    {
        await permissionService.EnsureHasSocialAccess();

        var result = await mainDbRepo.GetUsers()
                                     .Select(e => new ProfileDto
                                                  {
                                                      Id = e.MainGroupId,
                                                      UserId = e.Id,
                                                      MainGroupId = e.MainGroupId,
                                                      Email = e.Email,
                                                      PhoneNumber = e.PhoneNumber,
                                                      GoogleId = e.GoogleId,
                                                      AppleId = e.AppleId,
                                                      NickName = e.MainGroup.NickName,
                                                      Bio = e.MainGroup.Bio,
                                                      DefaultLanguageId = e.MainGroup.DefaultLanguageId,
                                                      TaxationCountryId = e.MainGroup.TaxationCountryId,
                                                      IsFeatured = e.IsFeatured,
                                                      IsBlocked = e.MainGroup.IsBlocked,
                                                      DeletedAt = e.MainGroup.DeletedAt,
                                                      CreatedTime = e.CreatedTime,
                                                      Files = e.MainGroup.Files,
                                                      Kpi = new ProfileKpiDto
                                                            {
                                                                VideoLikesCount = e.MainGroup.TotalLikes,
                                                                TotalVideoCount = e.MainGroup.TotalVideos,
                                                                FollowersCount = e.MainGroup.TotalFollowers
                                                            }
                                                  }
                                      )
                                     .ExecuteODataRequestWithCount(options);

        await fileStorage.InitUrls<Group>(result.Data);

        return result;
    }

    public async Task<ProfileShortDto[]> GetProfilesOrderedBy(
        string propertyName,
        bool? isFeatured,
        DateTime? startDate,
        DateTime? endDate,
        int take,
        int skip
    )
    {
        await permissionService.EnsureHasSocialAccess();

        var start = startDate?.StartOfDay() ?? DateTime.MinValue.ToUniversalTime();
        var end = endDate?.EndOfDay() ?? DateTime.MaxValue.ToUniversalTime();

        var query = await GetSortedGroupIds(propertyName, isFeatured, start, end);

        var groupIds = await query.Skip(skip).Take(take).ToArrayAsync();

        var profiles = await mainDbRepo.GetUsers().Where(e => groupIds.Contains(e.MainGroupId)).Select(ToProfileShortDto()).ToArrayAsync();

        await fileStorage.InitUrls<Group>(profiles);

        return groupIds.Join(profiles, i => i, e => e.MainGroupId, (_, e) => e).ToArray();

        Expression<Func<User, ProfileShortDto>> ToProfileShortDto()
        {
            return e => new ProfileShortDto
                        {
                            Id = e.MainGroupId,
                            MainGroupId = e.MainGroup.Id,
                            NickName = e.MainGroup.NickName,
                            Files = e.MainGroup.Files,
                            Kpi = new ProfileKpiDto
                                  {
                                      TotalVideoCount = e.MainGroup.Video.Count(v => v.CreatedTime >= start && end >= v.CreatedTime),
                                      FollowersCount = e.MainGroup.FollowerFollowing.Count(f => f.Time >= start && end >= f.Time),
                                      VideoLikesCount = e.MainGroup.Video
                                                         .SelectMany(v => v.Likes.Where(l => l.Time >= start && end >= l.Time))
                                                         .Count()
                                  }
                        };
        }
    }

    public async Task<ProfileKpiDto> GetProfileKpiByGroupId(long groupId)
    {
        await permissionService.EnsureHasSocialAccess();

        var profileKpi = await mainDbRepo.GetProfileKpis(groupId);

        return profileKpi;
    }

    public async Task<ResultWithCount<UserActivityDto>> GetUserActivity(
        ODataQueryOptions<UserActivityDto> options,
        long groupId,
        UserActionType? actionType
    )
    {
        await permissionService.EnsureHasSocialAccess();

        return await mainDbRepo.GetUserActivity()
                               .Where(e => e.GroupId == groupId)
                               .Where(e => actionType == null || e.ActionType == actionType)
                               .ProjectTo<UserActivityDto>(mapper.ConfigurationProvider)
                               .ExecuteODataRequestWithCount(options);
    }

    private async Task<IQueryable<long>> GetSortedGroupIds(string property, bool? isFeatured, DateTime startDate, DateTime endDate)
    {
        var kpiProperty = typeof(ProfileKpiDto).GetProperties()
                                               .FirstOrDefault(
                                                    p => string.Equals(p.Name, property, StringComparison.CurrentCultureIgnoreCase)
                                                );
        if (kpiProperty == null)
            throw new ValidationException($"Profile kpis does not have {property} property");

        var sortedQuery = kpiProperty.Name switch
                          {
                              nameof(ProfileKpiDto.FollowersCount) => mainDbRepo.GetGroupIdsByFollowers(startDate, endDate),
                              nameof(ProfileKpiDto.VideoLikesCount) => mainDbRepo.GetGroupIdsByLikes(startDate, endDate),
                              nameof(ProfileKpiDto.TotalVideoCount) => mainDbRepo.GetGroupIdsByTotalVideos(startDate, endDate),
                              _ => throw new ValidationException($"There is no sort option for {kpiProperty.Name}")
                          };

        if (!isFeatured.HasValue)
            return sortedQuery;

        var groupIds = await mainDbRepo.GetFeaturedGroupIds();

        return sortedQuery.Where(id => isFeatured.Value ? groupIds.Contains(id) : !groupIds.Contains(id));
    }
}