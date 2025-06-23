using System.Collections.Generic;
using System.Threading.Tasks;
using AssetServer.ModelBinders;
using AssetServer.Services;
using Common.Models.Files;
using Microsoft.AspNetCore.Mvc;

#pragma warning disable CS1998

namespace AssetServer.Controllers;

[ApiController]
[Route("api/")]
public class FileUploadController(IFileUploadService fileService) : Controller
{
    [HttpGet]
    [Route("File/PreUploadingUrl")]
    public async Task<ActionResult> GetFileUploadingUrl()
    {
        var result = fileService.GetTemporaryUploadingUrl();

        return Ok(result);
    }

    [HttpGet]
    [Route("File/PreConversionUrl/{fileExtension}")]
    public async Task<ActionResult> GetUrlForConversionAndUploading([FromRoute] string fileExtension)
    {
        var result = fileService.GetTemporaryConversionUrl(fileExtension);

        return Ok(result);
    }


    [HttpPost]
    [Route("File/{assetType}/{id}/MainFile/{platform}/Save/{uploadId}")]
    public async Task<ActionResult> SaveMainFile(
        [FromRoute] EntityModelType assetType,
        long id,
        Platform platform,
        string uploadId,
        [FromBody] Dictionary<string, string> tags
    )
    {
        var result = await fileService.SavePreloadedFile(
                         assetType.EntityType,
                         id,
                         FileType.MainFile,
                         platform,
                         null,
                         uploadId,
                         tags
                     );

        return Ok(result);
    }

    [HttpPost]
    [Route("File/{assetType}/{id}/Thumbnail/{resolution}/Save/{uploadId}")]
    public async Task<ActionResult> SaveImage(
        [FromRoute] EntityModelType assetType,
        long id,
        [ResolutionBinder] Resolution resolution,
        string uploadId,
        [FromBody] Dictionary<string, string> tags
    )
    {
        var result = await fileService.SavePreloadedFile(
                         assetType.EntityType,
                         id,
                         FileType.Thumbnail,
                         null,
                         resolution,
                         uploadId,
                         tags
                     );

        return Ok(result);
    }
}