using System;
using System.Threading.Tasks;
using Frever.AdminService.Core.Services.GeoClusters;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace Frever.AdminService.Api.Controllers;

[ApiController]
[Route("api/geo-cluster")]
public class GeoClusterController(IGeoClusterService geoClusterService) : ControllerBase
{
    private readonly IGeoClusterService _geoClusterService = geoClusterService ?? throw new ArgumentNullException(nameof(geoClusterService));

    [HttpGet]
    public async Task<IActionResult> GetAll(ODataQueryOptions<GeoClusterDto> options)
    {
        var result = await _geoClusterService.GetGeoClusters(options);

        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> SaveGeoCluster([FromBody] GeoClusterDto model)
    {
        await _geoClusterService.SaveGeoCluster(model);

        return NoContent();
    }

    [HttpPatch("{id}")]
    public async Task<IActionResult> UpdateBattleReward([FromRoute] long id, [FromBody] JObject model)
    {
        await _geoClusterService.UpdateGeoCluster(id, model);

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> UpdateBattleReward([FromRoute] long id)
    {
        await _geoClusterService.DeleteGeoCluster(id);

        return NoContent();
    }
}