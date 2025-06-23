using System;
using System.Threading.Tasks;
using Frever.AdminService.Core.Services.Social.Contracts;
using Frever.AdminService.Core.Utils;
using Frever.Shared.MainDb.Entities;
using Microsoft.AspNet.OData.Query;

namespace Frever.AdminService.Core.Services.Social;

public interface IProfileService
{
    Task<ResultWithCount<ProfileDto>> GetProfiles(ODataQueryOptions<ProfileDto> options);

    Task<ProfileShortDto[]> GetProfilesOrderedBy(
        string propertyName,
        bool? isFeatured,
        DateTime? startDate,
        DateTime? endDate,
        int take,
        int skip
    );

    Task<ProfileKpiDto> GetProfileKpiByGroupId(long groupId);

    Task<ResultWithCount<UserActivityDto>> GetUserActivity(
        ODataQueryOptions<UserActivityDto> options,
        long groupId,
        UserActionType? actionType
    );
}