using System;
using System.Threading.Tasks;
using Common.Models.Files;

namespace AssetServer.Services;

public interface IAssetAccessService
{
    Task<FileInfo> GetAssetFileFromDb(
        Type assetType,
        long id,
        Platform? platform,
        FileType fileType,
        Resolution? resolution = null,
        FileExtension? extension = null
    );
}