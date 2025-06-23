using System;
using System.Threading.Tasks;
using AssetServer.Services;
using Common.Models.Files;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetServer.Controllers;

[ApiController]
[Route("api/")]
public class FileDownloadController(IAssetService assetService) : Controller
{
    [HttpGet]
    [Route("Cdn/{assetType}/{id}/MainFile/{platform}/{version}")]
    public Task<IActionResult> RedirectToFileOnCdn(string assetType, long id, Platform platform, string version)
    {
        return GetCdnFileRedirect(
            assetType,
            id,
            version,
            platform,
            FileType.MainFile
        );
    }

    [HttpGet]
    [Route("CdnLink/{assetType}/{id}/MainFile/{platform}/{version}")]
    public async Task<IActionResult> GetLinkToFileOnCdn(string assetType, long id, Platform platform, string version)
    {
        var serviceResult = await assetService.GetCdnFileUrl(
                                assetType,
                                id,
                                version,
                                platform,
                                FileType.MainFile
                            );

        if (serviceResult.IsError)
            return StatusCode((int) serviceResult.StatusCode);
        return Ok(new {Ok = true, Link = serviceResult.Data});
    }

    [HttpGet]
    [AllowAnonymous]
    [Route("Cdn/{assetType}/{id}/Thumbnail/{resolution}/{version}")]
    public async Task<IActionResult> RedirectToImageOnCdn(string assetType, long id, string resolution, string version)
    {
        if (ParseResolutionInputArg(resolution, out var resolutionEnum))
            return await GetCdnFileRedirect(
                       assetType,
                       id,
                       version,
                       Platform.Undefined,
                       FileType.Thumbnail,
                       resolutionEnum
                   );

        return BadRequest($"Resolution {resolution} is not valid");
    }

    //TODO: drop in 1.9 version
    [HttpGet]
    [Route("Cdn/StorageFile/{version}/{*key}")]
    public async Task<IActionResult> RedirectToStorageFile([FromRoute] string version, [FromRoute] string key)
    {
        var serviceResult = await assetService.GetCdnStorageFileUrl(version, key, null);

        if (serviceResult.IsError)
            return StatusCode((int) serviceResult.StatusCode, serviceResult.ErrorMessage);

        return RedirectPermanent(serviceResult.Data);
    }

    [HttpGet]
    [Route("Cdn/StorageFiles/{version}/{extension}/{*key}")]
    public async Task<IActionResult> RedirectToStorageFile(
        [FromRoute] string version,
        [FromRoute] string key,
        [FromRoute] FileExtension extension
    )
    {
        var serviceResult = await assetService.GetCdnStorageFileUrl(version, key, extension);

        if (serviceResult.IsError)
            return StatusCode((int) serviceResult.StatusCode, serviceResult.ErrorMessage);

        return RedirectPermanent(serviceResult.Data);
    }

    [HttpGet]
    [Route("CdnLink/StorageFiles/{version}/{extension}/{*key}")]
    public async Task<IActionResult> RedirectToStorageFileLink(
        [FromRoute] string version,
        [FromRoute] string key,
        [FromRoute] FileExtension extension
    )
    {
        var serviceResult = await assetService.GetCdnStorageFileUrl(version, key, extension);

        return serviceResult.IsError
                   ? StatusCode((int) serviceResult.StatusCode, serviceResult.ErrorMessage)
                   : Ok(new {Ok = true, Link = serviceResult.Data});
    }

    private bool ParseResolutionInputArg(string input, out Resolution result)
    {
        return Enum.TryParse("_" + input, true, out result);
    }

    private async Task<IActionResult> GetCdnFileRedirect(
        string assetType,
        long id,
        string version,
        Platform platform,
        FileType fileType,
        Resolution? resolution = null
    )
    {
        var serviceResult = await assetService.GetCdnFileUrl(
                                assetType,
                                id,
                                version,
                                platform,
                                fileType,
                                resolution
                            );

        if (!serviceResult.IsError)
            return RedirectPermanent(serviceResult.Data);
        return StatusCode((int) serviceResult.StatusCode, serviceResult.ErrorMessage);
    }
}