using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using Common.Models;
using Common.Models.Files;

namespace Frever.Client.Shared.Files;

public class ParallelFileUploader : IFileUploader
{
    private static ISourceKeyParser[] _sourceKeyParsers;
    private readonly IExternalFileDownloader _externalFileDownloader;

    private readonly IFileStorageBackend _fileStorageBackend;
    private readonly IAdvancedFileStorageService _fileStorageService;
    private readonly ConcurrentBag<Task> _uploadTasks = [];


    public ParallelFileUploader(
        IAdvancedFileStorageService fileStorageService,
        IFileStorageBackend fileStorageBackend,
        IExternalFileDownloader externalFileDownloader
    )
    {
        _fileStorageService = fileStorageService ?? throw new ArgumentNullException(nameof(fileStorageService));
        _fileStorageBackend = fileStorageBackend ?? throw new ArgumentNullException(nameof(fileStorageBackend));
        _externalFileDownloader = externalFileDownloader ?? throw new ArgumentNullException(nameof(externalFileDownloader));

        _sourceKeyParsers ??= CreateSourceKeyParsers(fileStorageBackend.Bucket);
    }

    public async Task UploadFiles<TEntity>(IFileMetadataOwner entity)
        where TEntity : IFileMetadataConfigRoot
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (entity.Id == 0)
            throw new ArgumentException("Unable to upload file for entity without ID");

        var (isValid, errors) = await _fileStorageService.Validate<TEntity>(entity);
        if (!isValid)
            throw new ArgumentException($"Invalid files for {typeof(TEntity).Name}: {string.Join(". ", errors)}");

        foreach (var file in entity.Files.Where(f => f.Source != null))
        {
            file.Version = $"{DateTime.Now:yyyyMMdd_hhmmss}_{Guid.NewGuid():N}";
            file.Path = _fileStorageService.MakeFilePath<TEntity>(entity.Id, file);

            QueueUploading(file, file.Source);

            file.Source = null;
        }
    }

    public async Task UploadFilesAll<TEntity>(IEnumerable<IFileMetadataOwner> entities)
        where TEntity : IFileMetadataConfigRoot
    {
        ArgumentNullException.ThrowIfNull(entities);
        foreach (var entity in entities)
            await UploadFiles<TEntity>(entity);
    }

    public async Task WaitForCompletion()
    {
        await Task.WhenAll(_uploadTasks.ToArray()).ConfigureAwait(false);
    }

    private void QueueUploading(FileMetadata file, FileSourceInfo source)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(source);

        if (source.SourceBytes != null)
        {
            if (source.SourceBytes.Length == 0)
                throw new InvalidOperationException("Empty file source");

            async Task Upload()
            {
                await _fileStorageBackend.UploadToBucket(file.Path, source.SourceBytes);
            }

            _uploadTasks.Add(Upload());
            return;
        }

        if (!string.IsNullOrWhiteSpace(source.SourceFile))
        {
            foreach (var parser in _sourceKeyParsers)
                if (parser.TryParse(source.SourceFile, out var sourceInfo))
                {
                    async Task Upload()
                    {
                        await _fileStorageBackend.CopyFrom(sourceInfo.Bucket, sourceInfo.Key, file.Path);
                    }

                    _uploadTasks.Add(Upload());
                    return;
                }

            if (source.SourceFile.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                _uploadTasks.Add(UploadFromUrlToBucket(source.SourceFile, file.Path));
                return;
            }

            throw new InvalidOperationException("File source path has unsupported format");
        }

        throw new InvalidOperationException("File source is invalid");
    }

    private async Task UploadFromUrlToBucket(string url, string targetKey)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(url));
        if (string.IsNullOrWhiteSpace(targetKey))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(targetKey));

        var data = await _externalFileDownloader.Download(url);
        await _fileStorageBackend.UploadToBucket(targetKey, data);
    }

    private static ISourceKeyParser[] CreateSourceKeyParsers(string bucketName)
    {
        return
        [
            new CdnUrlSourceKeyParser(bucketName),
            new SignedUrlSourceKeyParser(),
            new S3UriSourceKeyParser(bucketName),
            new GenerationKeySourceKeyParser(),
            new UploadIdKeyParser(bucketName),
            new StorageReferenceParser(bucketName)
        ];
    }
}

public class SourceFileInfo
{
    public required string Bucket { get; set; }
    public required string Key { get; set; }
}

public interface ISourceKeyParser
{
    bool TryParse(string key, out SourceFileInfo sourceFileInfo);
}

public class SignedUrlSourceKeyParser : ISourceKeyParser
{
    private static readonly Regex Re = new("https://([\\w\\-]*)\\.[\\w-\\.]*(/.*)\\?", RegexOptions.IgnoreCase);

    public bool TryParse(string key, out SourceFileInfo sourceFileInfo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        var match = Re.Match(key);

        sourceFileInfo = null;

        if (!match.Success)
            return false;

        sourceFileInfo = new SourceFileInfo {Bucket = match.Groups[1].Value, Key = match.Groups[2].Value};
        return true;
    }
}

public class CdnUrlSourceKeyParser(string bucket) : ISourceKeyParser
{
    private static readonly Regex Re = new(
        "https:\\/\\/[\\w\\d\\-]*\\.frever-content\\.com([\\w\\d\\-\\/\\.]*)\\?{0,1}",
        RegexOptions.IgnoreCase
    );

    public bool TryParse(string key, out SourceFileInfo sourceFileInfo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        var match = Re.Match(key);

        sourceFileInfo = null;

        if (!match.Success)
            return false;

        sourceFileInfo = new SourceFileInfo {Key = match.Groups[1].Value, Bucket = bucket};
        return true;
    }
}

public class S3UriSourceKeyParser(string bucket) : ISourceKeyParser
{
    private static readonly Regex Re = new("s3://([\\w\\-]*)(/.*)", RegexOptions.IgnoreCase);

    public bool TryParse(string key, out SourceFileInfo sourceFileInfo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        var match = Re.Match(key);

        sourceFileInfo = null;

        if (!match.Success)
            return false;

        var sourceBucket = match.Groups[1].Value;
        if (string.IsNullOrWhiteSpace(sourceBucket) || sourceBucket == "-")
            sourceBucket = bucket;

        sourceFileInfo = new SourceFileInfo {Bucket = match.Groups[1].Value, Key = match.Groups[2].Value};
        return true;
    }
}

public class GenerationKeySourceKeyParser : ISourceKeyParser
{
    public bool TryParse(string key, out SourceFileInfo sourceFileInfo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        sourceFileInfo = null;

        if (key.Contains('%'))
            key = HttpUtility.UrlDecode(key);

        var parts = key.Split('|');
        if (parts.Length != 4)
            return false;

        sourceFileInfo = new SourceFileInfo {Bucket = parts[2], Key = parts[3]};

        return true;
    }
}

public class UploadIdKeyParser(string bucket) : ISourceKeyParser
{
    public bool TryParse(string key, out SourceFileInfo sourceFileInfo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        sourceFileInfo = null;

        if (!Guid.TryParse(key, out var guid))
            return false;

        sourceFileInfo = new SourceFileInfo {Bucket = bucket, Key = $"{Constants.TemporaryFolder}/{guid}"};

        return true;
    }
}

public class StorageReferenceParser(string bucket) : ISourceKeyParser
{
    public bool TryParse(string key, out SourceFileInfo sourceFileInfo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        sourceFileInfo = null;

        if (!StorageReference.TryDecode(key, bucket, out var sr))
            return false;

        sourceFileInfo = new SourceFileInfo {Bucket = sr.Bucket, Key = sr.Key};

        return true;
    }
}