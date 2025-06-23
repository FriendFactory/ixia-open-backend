using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Common.Models.Files;

namespace AssetServer.Shared.AssetCopying;

public interface IAssetCopyingService
{
    /// <summary>
    ///     Queue file copying and return new version of copied file.
    ///     Also updates <see cref="Version" /> of <paramref name="file" />.
    /// </summary>
    Task QueueAssetFileCopying(Type assetType, long id, FileInfo file, Dictionary<string, string> tags = null);

    Task QueueFileCopyingBetweenTypes(
        Type fromAssetType,
        Type toAssetType,
        long id,
        FileInfo file,
        Dictionary<string, string> tags = null
    );

    string GenerateNewVersion();
}