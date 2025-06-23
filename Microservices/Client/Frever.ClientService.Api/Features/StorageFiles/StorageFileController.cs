using System;
using System.Threading.Tasks;
using Frever.Client.Core.Features.StorageFiles;
using Frever.ClientService.Contract.Common;
using Frever.ClientService.Contract.StorageFiles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Frever.ClientService.Api.Features.StorageFiles;

[Authorize]
[Route("api/StorageFile")]
public class StorageFileController(IStorageFileService storageFileService) : ControllerBase
{
    private readonly IStorageFileService _storageFileService = storageFileService ?? throw new ArgumentNullException(nameof(storageFileService));

    [HttpGet]
    [Route("/api/file/uploading-url")]
    public async Task<ActionResult> GetFileUploadingUrl()
    {
        var result = await _storageFileService.GetTemporaryUploadingUrl(null);

        return Ok(result);
    }

    [HttpGet]
    [Route("/api/file/uploading-url/{extension}")]
    public async Task<ActionResult> GetFileUploadingUrl(FileExtension? extension)
    {
        var result = await _storageFileService.GetTemporaryUploadingUrl(extension);

        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(StorageFileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStorageFileByKey([FromBody] StorageFileShortDto model)
    {
        var result = await _storageFileService.GetStorageFileByKey(model);
        if (result == null)
            return NotFound();

        return Ok(result);
    }

    [HttpGet]
    [Route("{version}/{extension}/{*key}")]
    public async Task<IActionResult> RedirectToStorageFile(
        [FromRoute] string version,
        [FromRoute] FileExtension extension,
        [FromRoute] string key
    )
    {
        var result = await _storageFileService.GetCdnStorageFileUrl(version, key, extension);

        return RedirectPermanent(result);
    }
}