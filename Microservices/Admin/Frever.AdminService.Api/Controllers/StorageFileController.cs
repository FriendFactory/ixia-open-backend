using System;
using System.Threading.Tasks;
using Frever.AdminService.Core.Services.StorageFiles;
using Frever.AdminService.Core.Services.StorageFiles.Contracts;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNetCore.Mvc;

#pragma warning disable CA2007

namespace Frever.AdminService.Api.Controllers;

[Route("api/storage-file/moderation")]
[ApiController]
public class StorageFileController(IStorageFileService storageFileService) : ControllerBase
{
    private readonly IStorageFileService _storageFileService = storageFileService ?? throw new ArgumentNullException(nameof(storageFileService));

    [HttpGet]
    public async Task<IActionResult> GetAll(ODataQueryOptions<StorageFileDto> options)
    {
        var result = await _storageFileService.GetAll(options);

        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> SaveStorageFile(UploadStorageFileModel model)
    {
        await _storageFileService.SaveStorageFile(model);

        return Ok();
    }

    [HttpDelete]
    [Route("{id}")]
    public async Task<IActionResult> DeleteStorageFile(long id)
    {
        await _storageFileService.DeleteStorageFile(id);

        return NoContent();
    }
}