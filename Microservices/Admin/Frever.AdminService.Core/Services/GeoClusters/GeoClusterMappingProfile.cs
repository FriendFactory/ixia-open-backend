using Frever.Shared.MainDb.Entities;
using Profile = AutoMapper.Profile;

namespace Frever.AdminService.Core.Services.GeoClusters;

// ReSharper disable once UnusedType.Global
public class GeoClusterMappingProfile : Profile
{
    public GeoClusterMappingProfile()
    {
        CreateMap<GeoClusterDto, GeoCluster>().ReverseMap();
    }
}