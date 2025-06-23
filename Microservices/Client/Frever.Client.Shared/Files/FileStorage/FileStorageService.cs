using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Models.Files;

namespace Frever.Client.Shared.Files;

public class FileStorageService(
    IEnumerable<IEntityFileMetadataConfiguration> fileMetadataConfigurations,
    IFileStorageBackend storageBackend,
    IFileUploaderFactory fileUploaderFactory
) : IFileStorageService, IAdvancedFileStorageService
{
    public Task<(bool IsValid, List<string> Errors)> Validate<T>(IFileMetadataOwner entity)
        where T : IFileMetadataConfigRoot
    {
        var config = GetFileMetadataConfig<T>();
        var result = config.Validate(entity);

        return Task.FromResult(result);
    }

    public Task<(bool IsValid, List<string> Errors)> ValidateFileTypes<T>(IFileMetadataOwner entity)
        where T : IFileMetadataConfigRoot
    {
        var config = GetFileMetadataConfig<T>();
        var result = config.ValidateFileTypes(entity);

        return Task.FromResult(result);
    }

    public Task<(bool IsValid, List<string> Errors)> ValidateFile<T>(IFileMetadataOwner entity, FileMetadata file)
        where T : IFileMetadataConfigRoot
    {
        var config = GetFileMetadataConfig<T>();
        var result = config.ValidateFile(entity, file);

        return Task.FromResult(result);
    }

    public string MakeFilePath<TEntity>(long id, FileMetadata file)
        where TEntity : IFileMetadataConfigRoot
    {
        ArgumentNullException.ThrowIfNull(file);
        if (id == 0)
            throw new ArgumentException("Unable to make path without ID");

        var config = GetFileMetadataConfig<TEntity>();
        return config.MakeFilePath(id, file);
    }

    public Task InitUrls<T>(IEnumerable<IFileMetadataOwner> entities)
        where T : IFileMetadataConfigRoot
    {
        var fileConfig = GetFileMetadataConfig<T>();

        foreach (var entity in entities)
        {
            foreach (var file in entity.Files ?? [])
            {
                var signed = fileConfig.NeedSignedUrl(entity.Id, file);

                file.Url = storageBackend.MakeCdnUrl(file.Path, signed);
                file.Source = null;
            }
        }

        return Task.CompletedTask;
    }

    public IFileUploader CreateFileUploader()
    {
        return fileUploaderFactory.CreateFileUploader();
    }

    private IEntityFileMetadataConfiguration GetFileMetadataConfig<TEntity>()
        where TEntity : IFileMetadataConfigRoot
    {
        var configuration = fileMetadataConfigurations.FirstOrDefault(c => c.EntityType == typeof(TEntity));
        if (configuration == null)
            throw new InvalidOperationException($"File metadata configuration for {typeof(TEntity).Name} not found");

        return configuration;
    }
}