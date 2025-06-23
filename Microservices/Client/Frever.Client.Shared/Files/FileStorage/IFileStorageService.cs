using System.Collections.Generic;
using System.Threading.Tasks;
using Common.Models.Files;

namespace Frever.Client.Shared.Files;

public interface IFileStorageService
{
    /// <summary>
    ///     Inits URLs for files.
    ///     Actual configuration of URL is determined by generic parameter, not by actual entity type.
    /// </summary>
    /// <param name="entities">The entities to initialize file URLs.</param>
    Task InitUrls<TEntity>(IEnumerable<IFileMetadataOwner> entities)
        where TEntity : IFileMetadataConfigRoot;

    IFileUploader CreateFileUploader();
}

public interface IAdvancedFileStorageService
{
    Task<(bool IsValid, List<string> Errors)> Validate<T>(IFileMetadataOwner entity)
        where T : IFileMetadataConfigRoot;

    Task<(bool IsValid, List<string> Errors)> ValidateFileTypes<T>(IFileMetadataOwner entity)
        where T : IFileMetadataConfigRoot;

    Task<(bool IsValid, List<string> Errors)> ValidateFile<T>(IFileMetadataOwner entity, FileMetadata file)
        where T : IFileMetadataConfigRoot;

    string MakeFilePath<TEntity>(long id, FileMetadata file)
        where TEntity : IFileMetadataConfigRoot;
}

public static class FileStorageServiceExtensions
{
    public static Task InitUrls<T>(this IFileStorageService service, IFileMetadataOwner entity)
        where T : IFileMetadataConfigRoot
    {
        return service.InitUrls<T>([entity]);
    }
}