using System.Threading.Tasks;
using AssetServer.ModelBinders;
using AssetServer.Services;
using Microsoft.AspNetCore.Mvc;

namespace AssetServer.Controllers;

[ApiController]
[Route("api/")]
public class FileDeleteController(IDeleteService deleteService) : Controller
{
    [HttpDelete]
    [Route("Asset/{assetType}/{id}")]
    public async Task<ActionResult> DeleteAsset([FromRoute] EntityModelType assetType, long id)
    {
        await deleteService.DeleteAsset(assetType.EntityType, id);

        return NoContent();
    }
}