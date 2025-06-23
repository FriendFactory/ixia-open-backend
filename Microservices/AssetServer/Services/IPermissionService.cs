using System;
using System.Threading.Tasks;
using AssetServer.Services.Storage;
using Common.Models.Files;

namespace AssetServer.Services;

public interface IPermissionService
{
    /// <summary>
    ///     New and <s>better</s> fixed version of permission checking.
    ///     1. If asset type is not IGroupAccessible -> asset file is public available
    ///     2. If asset has no tags -> check DB as fallback
    ///     3. If asset has public group in groups tag
    ///     4. If asset has common groups with user
    ///     5. If asset group is accessible to user
    ///     6. If asset is user sound - should be accessible if used by any level
    ///     7. TODO: Hotfix: if asset type is in list of public asset types
    /// </summary>
    Task<bool> HasPermissions(Type assetType, long id, FileTagInfo tags);

    /// <summary>
    ///     To offload server from signing expensive operation we open access for those files
    /// </summary>
    bool IsFileProtected(Type assetType, FileType fileType);
}