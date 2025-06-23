using AutoMapper;
using Frever.AdminService.Core.Services.HashtagModeration.Contracts;
using Frever.Shared.MainDb.Entities;

namespace Frever.AdminService.Core.Installers;

public class AutoMapperConfiguration : Profile
{
    public AutoMapperConfiguration()
    {
        CreateMap<Hashtag, HashtagInfo>();
    }
}