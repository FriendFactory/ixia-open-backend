using System.Linq;
using System.Threading.Tasks;
using Frever.AdminService.Core.Utils;
using Microsoft.AspNet.OData.Query;
using Newtonsoft.Json.Linq;

namespace Frever.AdminService.Core.Services.GeoClusters;

public interface IGeoClusterService
{
    Task<ResultWithCount<GeoClusterDto>> GetGeoClusters(ODataQueryOptions<GeoClusterDto> options);

    Task SaveGeoCluster(GeoClusterDto model);

    Task UpdateGeoCluster(long id, JObject model);

    Task DeleteGeoCluster(long id);
}