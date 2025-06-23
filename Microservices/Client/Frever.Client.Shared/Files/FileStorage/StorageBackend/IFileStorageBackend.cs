using System.Threading.Tasks;

namespace Frever.Client.Shared.Files;

public interface IFileStorageBackend
{
    string Bucket { get; }

    /// <summary>
    ///     Makes CDN URL for provided file path, optionally signed.
    /// </summary>
    string MakeCdnUrl(string filePath, bool signed = false);

    Task UploadToBucket(string targetKey, byte[] content);

    Task CopyFrom(string sourceBucket, string sourceKey, string targetKey);
}