using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Common.Models.Files;

namespace Frever.Client.Shared.Files;

public interface IEntityFileMetadataConfiguration
{
    Type EntityType { get; }

    (bool IsValid, List<string> Errors) Validate(IFileMetadataOwner entity);

    /// <summary>
    ///     Checks if entity contains valid files.
    ///     Validation is somehow limited because actual options varies, but:
    ///     - for new entities (with Id == 0) it validates if entity contains all required files
    ///     - for all entities it validates if entity doesn't contain extra file types
    ///     - for all entities it validates if entity doesn't contain file duplicates
    /// </summary>
    (bool IsValid, List<string> Errors) ValidateFileTypes(IFileMetadataOwner entity);

    (bool IsValid, List<string> Errors) ValidateFile(IFileMetadataOwner entity, FileMetadata file);

    bool NeedSignedUrl(long id, FileMetadata file);

    Task<bool> HasPermission(long id, FileMetadata file);

    string MakeFilePath(long id, FileMetadata file);
}