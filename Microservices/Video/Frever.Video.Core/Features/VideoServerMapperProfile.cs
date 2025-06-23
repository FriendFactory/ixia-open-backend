using AutoMapper;
using Frever.Shared.MainDb.Entities;
using Frever.Video.Contract;

namespace Frever.Video.Core.Features;

internal class VideoServerMapperProfile : Profile
{
    public VideoServerMapperProfile()
    {
        CreateMap<Group, TaggedGroup>()
           .ForMember(e => e.GroupId, opt => opt.MapFrom(e => e.Id))
           .ForMember(e => e.GroupNickname, opt => opt.MapFrom(e => e.NickName));
    }
}