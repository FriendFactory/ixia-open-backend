using System.Collections.Generic;
using System.Threading.Tasks;

namespace AssetServer.Services.Storage;

public interface IStorageService
{
    string GetTempUrlForUploadingFile(string filePathOnBucket);

    string GetTempUrlForReadingFile(string filePathOnBucket);

    /// <summary>
    ///     Creates signed URL which allows HEAD method (to check if file exists without downloading its content)
    /// </summary>
    string GetTempUrlForCheckingFileExistence(string filePathOnBucket);

    Task<byte[]> GetFileAsync(string filePath); //todo:change to stream

    Task<string> CopyFileAsync(string fromPath, string toPath, Dictionary<string, string> tags = null);

    Task<long> GetFileSizeKb(string path);

    Task DeleteDirectoryWithAllFiles(string directory);

    Task<S3FileInfo> GetFileInfo(string key);
}