using Frever.AdminService.Core.Services.Social.Contracts;
using Frever.Shared.MainDb.Entities;
using Profile = AutoMapper.Profile;

namespace Frever.AdminService.Core.Services.Social;

// ReSharper disable once UnusedType.Global
public class SocialMappingProfile : Profile
{
    public SocialMappingProfile()
    {
        CreateMap<UserActivity, UserActivityDto>();
    }
}