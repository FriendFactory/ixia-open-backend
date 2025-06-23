using System.Collections.Generic;
using System.Threading.Tasks;
using Common.Models.Files;

namespace Frever.Client.Shared.Files;

public interface IFileUploader
{
    /// <summary>
    ///     Check all files for entity and initiates upload.
    ///     Generic parameter TEntity is used for determining entity configuration instead of actual entity type.
    /// </summary>
    Task UploadFiles<TEntity>(IFileMetadataOwner entity)
        where TEntity : IFileMetadataConfigRoot;

    /// <summary>
    ///     Check all files for entity and initiates upload.
    ///     Generic parameter TEntity is used for determining entity configuration instead of actual entity type.
    /// </summary>
    Task UploadFilesAll<TEntity>(IEnumerable<IFileMetadataOwner> entities)
        where TEntity : IFileMetadataConfigRoot;

    /// <summary>
    ///     Waits until all upload complete.
    /// </summary>
    Task WaitForCompletion();
}