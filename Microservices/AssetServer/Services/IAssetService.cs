using System.Threading.Tasks;
using AssetServer.Models;
using Common.Models.Files;

namespace AssetServer.Services;

public interface IAssetService
{
    Task<ServiceResult<string>> GetCdnFileUrl(
        string assetTypeName,
        long id,
        string version,
        Platform platform,
        FileType fileType,
        Resolution? resolution = null
    );

    Task<ServiceResult<string>> GetCdnStorageFileUrl(string version, string key, FileExtension? extension);
}