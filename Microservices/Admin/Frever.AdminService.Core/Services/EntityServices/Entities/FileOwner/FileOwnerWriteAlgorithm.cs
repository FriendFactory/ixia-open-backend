using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AssetServer.Shared.AssetCopying;
using AuthServerShared;
using Common.Models;
using Common.Models.Database.Interfaces;
using Common.Models.Files;
using Frever.AdminService.Core.Services.ModelSettingsProviders;
using Frever.AdminService.Core.UoW;
using Frever.AdminService.Core.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Frever.AdminService.Core.Services.EntityServices;

/// <summary>
///     Copies files specified in entity due creating or updating and deletes files on delete.
/// </summary>
internal class FileOwnerWriteAlgorithm<TEntity>(
    UserInfo user,
    IEnumerable<IEntityValidator<TEntity>> validators,
    ILoggerFactory loggerFactory,
    AssetGroupProvider assetGroupProvider,
    IUnitOfWork unitOfWork,
    IEntityReadAlgorithm<TEntity> readAlgorithm,
    IAssetCopyingService assetService,
    IEntityLifeCycle<TEntity> entityLifeCycle
) : DefaultEntityWriteAlgorithm<TEntity>(
    user,
    validators,
    loggerFactory,
    assetGroupProvider,
    unitOfWork,
    readAlgorithm,
    entityLifeCycle
)
    where TEntity : class, IEntity, IFileOwner
{
    public override async Task PreSave(TEntity entity, CallContext context)
    {
        await base.PreSave(entity, context);

        context.ModifyFeature(
            (CopyFilesFeature<TEntity> f) => f.AddEntityFiles(entity, (entity.Files ?? Enumerable.Empty<FileInfo>()).ToArray())
        );

        // Restore file info from database
        // to prevent saving non-uploaded files or erasing existing
        entity.Files = entity.Id == 0
                           ? []
                           : (await ReadAlgorithm.GetOneNoDependencies(entity.Id)
                                                 .AsNoTracking()
                                                 .Select(e => new {e.Files})
                                                 .SingleOrDefaultAsyncSafe())?.Files ?? [];
    }

    public override async Task AfterSaveChanges(TEntity entity, CallContext context)
    {
        await base.AfterSaveChanges(entity, context);

        // Delete files if entity were marked for deletion.
        // Don't check context.Operation here because entity could be deleted
        // as part of update operation
        if (IsEntityMarkedForDeletion(entity, context))
        {
            await DeleteFiles(entity, context);
        }
        else
        {
            entity.Files = await CopyUploadedFiles(entity, context);
            await UnitOfWork.SaveChanges();
        }
    }

    private async Task<List<FileInfo>> CopyUploadedFiles(TEntity entity, CallContext context)
    {
        var receivedFiles = context.GetFeature<CopyFilesFeature<TEntity>>()?.GetEntityFiles(entity).ToList() ?? [];
        var dbFiles = entity.Files;

        var filesToUpload = receivedFiles.Where(
                                              received =>
                                              {
                                                  var existing = dbFiles.FirstOrDefault(
                                                      df => FileInfoMergeEqualityComparer.Instance.Equals(df, received)
                                                  );

                                                  return NeedsReUpload(existing?.Source, received.Source);
                                              }
                                          )
                                         .ToArray();

        if (filesToUpload.Length != 0)
        {
            var tags = await GetFileTags(entity, context);

            foreach (var item in filesToUpload.Where(u => u != null))
                await CopyFileAsync(entity.Id, item, tags);
        }

        var updatedFiles = MergeFileInfoLists(filesToUpload, dbFiles);

        return updatedFiles;
    }

    private Task DeleteFiles(TEntity entity, CallContext context)
    {
        return Task.CompletedTask;
    }

    protected virtual Task<long?> GetLevelId(TEntity entity, CallContext context)
    {
        return Task.FromResult<long?>(null);
    }

    protected virtual async Task<Dictionary<string, string>> GetFileTags(TEntity entity, CallContext context)
    {
        if (entity is not IGroupAccessible ga)
            return new Dictionary<string, string> {{FileTags.GROUP_ID, Constants.PublicAccessGroupId.ToString()}};

        var tags = new Dictionary<string, string> {{FileTags.GROUP_ID, ga.GroupId.ToString()}};

        var levelId = await GetLevelId(entity, context);

        if (levelId.HasValue)
            tags.Add(FileTags.LEVEL_ID, levelId.Value.ToString());

        return tags;
    }

    private static bool NeedsReUpload(FileSource existingSource, FileSource newSource)
    {
        if (!string.IsNullOrWhiteSpace(newSource?.UploadId))
            return newSource.UploadId != existingSource?.UploadId;

        if (newSource?.CopyFrom != null)
            return newSource.CopyFrom?.Id != existingSource?.CopyFrom?.Id ||
                   newSource.CopyFrom?.Version != existingSource?.CopyFrom?.Version;

        return false;
    }

    /// <summary>
    ///     Merge two list of files.
    ///     Overrides matched files in existing list with files in new list.
    /// </summary>
    private static List<FileInfo> MergeFileInfoLists(IEnumerable<FileInfo> newFiles, IEnumerable<FileInfo> existingFiles)
    {
        var merged = new HashSet<FileInfo>(FileInfoMergeEqualityComparer.Instance);

        foreach (var fi in newFiles ?? [])
            merged.Add(fi);

        foreach (var fi in existingFiles ?? [])
            merged.Add(fi);

        return merged.Where(f => f != null).ToList();
    }

    /// <summary>
    ///     Copies upload and returns file version.
    /// </summary>
    private async Task CopyFileAsync(long modelId, FileInfo fileData, Dictionary<string, string> tags)
    {
        try
        {
            fileData.Version = assetService.GenerateNewVersion();

            await assetService.QueueAssetFileCopying(typeof(TEntity), modelId, fileData, tags);
        }
        catch (Exception ex)
        {
            var fileInfoDetails = $"{fileData?.File} {fileData?.Resolution?.ToString()}";

            throw new EntityWriteException(
                $"Unable to copy file for model {typeof(TEntity).Name} Id:{modelId};{fileInfoDetails}. " + "Source file not exists.",
                ex,
                WriteOperation.Create
            );
        }
    }
}