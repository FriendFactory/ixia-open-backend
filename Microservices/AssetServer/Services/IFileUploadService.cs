using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AssetServer.Models;
using Common.Models.Files;

namespace AssetServer.Services;

public interface IFileUploadService
{
    InitUploadModel GetTemporaryUploadingUrl();

    InitConversionModel GetTemporaryConversionUrl(string fileExtension);

    Task<SavePreloadedFileResp> SavePreloadedFile(
        Type assetType,
        long id,
        FileType fileType,
        Platform? platform,
        Resolution? resolution,
        string uploadId,
        Dictionary<string, string> tags = null
    );
}