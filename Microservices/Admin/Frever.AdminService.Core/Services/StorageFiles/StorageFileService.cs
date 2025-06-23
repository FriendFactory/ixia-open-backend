using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.SQS;
using AssetServer.Shared.AssetCopying;
using AssetServer.Shared.Messages;
using AssetStoragePathProviding;
using AuthServer.Permissions.Services;
using Common.Infrastructure;
using Common.Infrastructure.Aws;
using Common.Models;
using Common.Models.Files;
using FluentValidation;
using Frever.AdminService.Core.Services.StorageFiles.Contracts;
using Frever.AdminService.Core.Utils;
using Frever.Cache.Resetting;
using Frever.Shared.MainDb;
using Frever.Shared.MainDb.Entities;
using Microsoft.AspNet.OData.Query;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Frever.AdminService.Core.Services.StorageFiles;

public class StorageFileService(
    AssetCopyingOptions copyingOptions,
    IAmazonS3 s3,
    IAmazonSQS sqs,
    IFileBucketPathService fileBucketPath,
    IWriteDb db,
    ICacheReset cacheReset,
    IValidator<UploadStorageFileModel> validator,
    ILoggerFactory loggerFactory,
    IUserPermissionService permissionService
) : IStorageFileService
{
    private readonly ICacheReset _cacheReset = cacheReset ?? throw new ArgumentNullException(nameof(cacheReset));
    private readonly AssetCopyingOptions _copyingOptions = copyingOptions ?? throw new ArgumentNullException(nameof(copyingOptions));
    private readonly IWriteDb _db = db ?? throw new ArgumentNullException(nameof(db));
    private readonly IFileBucketPathService _fileBucketPath = fileBucketPath ?? throw new ArgumentNullException(nameof(fileBucketPath));
    private readonly IAmazonS3 _s3 = s3 ?? throw new ArgumentNullException(nameof(s3));
    private readonly IAmazonSQS _sqs = sqs ?? throw new ArgumentNullException(nameof(sqs));
    private readonly IValidator<UploadStorageFileModel> _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    private readonly IUserPermissionService _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));

    private readonly ILogger _log = loggerFactory.CreateLogger("Frever.ImageService");

    public async Task<ResultWithCount<StorageFileDto>> GetAll(ODataQueryOptions<StorageFileDto> options)
    {
        await _permissionService.EnsureHasCategoryReadAccess();

        return await _db.StorageFile.Select(
                             e => new StorageFileDto
                                  {
                                      Id = e.Id,
                                      Key = e.Key,
                                      Version = e.Version,
                                      Extension = Enum.Parse<FileExtension>(e.Extension)
                                  }
                         )
                        .ExecuteODataRequestWithCount(options);
    }

    public async Task SaveStorageFile(UploadStorageFileModel model)
    {
        await _permissionService.EnsureHasCategoryFullAccess();

        await _validator.ValidateAndThrowAsync(model);

        var file = await _db.StorageFile.FirstOrDefaultAsync(e => e.Id == model.Id);
        if (file is null)
        {
            file = new StorageFile();
            await _db.StorageFile.AddAsync(file);
        }

        if (model.Id == 0)
            file.Key = model.Key;

        var version = $"{DateTime.Now:yyyyMMddTHHmmss}U{Guid.NewGuid():N}";

        file.Version = version;
        file.Extension = model.Extension.ToString();

        await UploadFile(model, version);

        await _db.SaveChangesAsync();

        await _cacheReset.ResetOnDependencyChange(typeof(StorageFile), null);
    }

    public async Task DeleteStorageFile(long id)
    {
        await _permissionService.EnsureHasCategoryFullAccess();

        var file = await _db.StorageFile.FirstOrDefaultAsync(e => e.Id == id);
        if (file == null)
            throw AppErrorWithStatusCodeException.BadRequest($"File ID={id} is not found or not accessible", "ImageNotFound");

        _db.StorageFile.Remove(file);

        await _db.SaveChangesAsync();

        var folder = Constants.FilesFolder + "/" + file.Key;

        await _s3.DeleteFolder(_copyingOptions.BucketName, folder, m => _log.LogTrace(m));
    }

    private async Task UploadFile(UploadStorageFileModel model, string version)
    {
        var targetFilePath = _fileBucketPath.GetPathToVersionedStorageFile(model.Key, version, model.Extension);

        var sourceFilePath = _fileBucketPath.GetPathToTempUploadFile(model.UploadId);

        var message = new CopyAssetMessage
                      {
                          Bucket = _copyingOptions.BucketName,
                          Tags = new Dictionary<string, string>(),
                          FromKey = sourceFilePath,
                          ToKey = targetFilePath
                      };

        try
        {
            var messageBody = JsonConvert.SerializeObject(message);

            _log.LogDebug("Send image copy message: {Body}", messageBody);
            await _sqs.SendMessageAsync(_copyingOptions.AssetCopyingQueueUrl, messageBody);
        }
        catch (Exception e)
        {
            _log.LogError(e, "Error sending message to asset copying queue");

            throw;
        }
    }
}