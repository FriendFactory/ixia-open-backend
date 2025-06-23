using System.Collections.Generic;
using System.Threading.Tasks;
using AssetServer.ModelBinders;
using AssetServer.Services;
using Common.Models.Files;
using Microsoft.AspNetCore.Mvc;

namespace AssetServer.Controllers;

[ApiController]
[Route("api/")]
public class FileCopyController(ICopyFileService copyFileService) : Controller
{
    [HttpPost]
    [Route("File/{assetType}/{id}/MainFile/{platform}/CopyTo/{destinationAssetId}")]
    public async Task<ActionResult> CopyMainFile(
        [FromRoute] EntityModelType assetType,
        long id,
        Platform platform,
        long destinationAssetId,
        [FromBody] Dictionary<string, string> tags
    )
    {
        var resp = await copyFileService.CopyFile(
                       assetType.EntityType,
                       id,
                       FileType.MainFile,
                       platform,
                       null,
                       destinationAssetId,
                       tags
                   );

        return Ok(resp);
    }

    [HttpPost]
    [Route("File/{assetType}/{id}/Thumbnail/{resolution}/CopyTo/{destinationAssetId}")]
    public async Task<ActionResult> CopyImage(
        [FromRoute] EntityModelType assetType,
        long id,
        [ResolutionBinder] Resolution resolution,
        long destinationAssetId,
        [FromBody] Dictionary<string, string> tags
    )
    {
        var resp = await copyFileService.CopyFile(
                       assetType.EntityType,
                       id,
                       FileType.Thumbnail,
                       null,
                       resolution,
                       destinationAssetId,
                       tags
                   );

        return Ok(resp);
    }
}