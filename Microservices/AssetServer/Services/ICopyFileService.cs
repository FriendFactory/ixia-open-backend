using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Common.Models.Files;

namespace AssetServer.Services;

public interface ICopyFileService
{
    Task<CopyFileResp> CopyFile(
        Type assetType,
        long sourceAssetId,
        FileType fileType,
        Platform? platform,
        Resolution? resolution,
        long destAssetId,
        Dictionary<string, string> tags = null
    );
}