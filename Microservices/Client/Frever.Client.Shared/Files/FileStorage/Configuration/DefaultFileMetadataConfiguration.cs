using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Common.Models.Files;

namespace Frever.Client.Shared.Files;

public class FileConfig(string type, string extension, bool isPublic = true, string name = null)
{
    public string Type { get; } = type;
    public string Extension { get; } = extension;
    public string Name { get; } = name;
    public bool IsPublic { get; } = isPublic;
}

public class DefaultFileMetadataConfiguration<T> : IEntityFileMetadataConfiguration
    where T : class, IFileMetadataConfigRoot
{
    protected const string FileRootFolder = "ixia/files";
    private readonly List<FileConfig> _configs = [];

    public Type EntityType => typeof(T);

    public (bool IsValid, List<string> Errors) Validate(IFileMetadataOwner entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var errors = new List<string>();

        var (_, fileTypeErrors) = ValidateFileTypes(entity);
        errors.AddRange(fileTypeErrors);

        foreach (var file in entity.Files)
        {
            var (_, fileErrors) = ValidateFile(entity, file);
            errors.AddRange(fileErrors);
        }

        return errors.Count == 0 ? (true, []) : (false, errors);
    }


    public (bool IsValid, List<string> Errors) ValidateFileTypes(IFileMetadataOwner entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var errors = new List<string>();

        foreach (var config in _configs)
        {
            var matchingFiles = entity.Files.Where(f => f.Type == config.Type).ToList();

            if (entity.Id == 0 && matchingFiles.Count != 1)
                errors.Add($"Expected one file of type {config.Type}, but found {matchingFiles.Count}");
            if (entity.Id != 0 && matchingFiles.Count > 1)
                errors.Add($"Duplicate file of type {config.Type}");
        }

        foreach (var file in entity.Files)
        {
            var config = _configs.FirstOrDefault(c => c.Type == file.Type);
            if (config == null)
                errors.Add($"Unexpected file type {file.Type}");
        }

        return errors.Count == 0 ? (true, []) : (false, errors);
    }

    public (bool IsValid, List<string> Errors) ValidateFile(IFileMetadataOwner entity, FileMetadata file)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(file);

        if (!entity.Files.Contains(file))
            return (false, ["File belongs to other entity"]);

        var config = _configs.FirstOrDefault(c => c.Type.Equals(file.Type, StringComparison.Ordinal));
        if (config == null)
            return (false, ["Unknown file type"]);

        if (entity.Id == 0 && file.Source == null)
            return (false, ["File source is required"]);

        return (true, []);
    }

    public bool NeedSignedUrl(long id, FileMetadata info)
    {
        var config = _configs.FirstOrDefault(r => r.Type == info.Type);
        if (config != null)
            return !config.IsPublic;

        return !info.Type.StartsWith(FileMetadata.KnowFileTypePrefixThumbnail);
    }

    public virtual Task<bool> HasPermission(long id, FileMetadata info)
    {
        return Task.FromResult(true);
    }

    public virtual string MakeFilePath(long id, FileMetadata info)
    {
        var config = _configs.FirstOrDefault(r => r.Type == info.Type);
        if (config == null)
            throw new InvalidOperationException($"No config defined for file type '{info.Type}'");

        if (id == 0)
            throw new InvalidOperationException("Can't create path for ID=0");

        if (string.IsNullOrWhiteSpace(info.Version))
            throw new InvalidOperationException("Can't create a path for file without version");

        var fileName = config.Name ?? "content";
        var prefix = config.IsPublic ? "Thumbnail_" : ""; // Required to allow anonymous access to public files

        return $"{FileRootFolder}/{typeof(T).Name}/{id}/{info.Type}/{info.Version}/{prefix}{fileName}.{config.Extension}";
    }

    public DefaultFileMetadataConfiguration<T> AddFile(string type, string extension, bool isPublic = true)
    {
        _configs.Add(new FileConfig(type, extension, isPublic, "content"));
        return this;
    }

    public DefaultFileMetadataConfiguration<T> AddMainFile(string extension, bool isPublic = false)
    {
        return AddFile(FileMetadata.KnowFileTypeMain, extension, isPublic);
    }

    public DefaultFileMetadataConfiguration<T> AddThumbnail(int resolution, string extension)
    {
        return AddFile($"{FileMetadata.KnowFileTypePrefixThumbnail}{resolution}", extension);
    }
}

public static class FileMetadataExtensions
{
    public static FileMetadata? Main(this IEnumerable<FileMetadata> files)
    {
        ArgumentNullException.ThrowIfNull(files);
        return files.FirstOrDefault(f => f.Type == FileMetadata.KnowFileTypeMain);
    }
}

public static class FileMetadataMappingExtensions
{
    public static IMappingExpression<TSource, TDestination> MapFileMetadata<TSource, TDestination>(
        this IMappingExpression<TSource, TDestination> map
    )
        where TSource : IFileMetadataOwner
        where TDestination : IFileMetadataOwner
    {
        ArgumentNullException.ThrowIfNull(map);

        return map.ForMember(
            dst => dst.Files,
            opt => opt.MapFrom(
                (src, dst) =>
                {
                    src.Files ??= [];
                    dst.Files ??= [];

                    return MergeFileMetadata(src.Files, dst.Files);
                }
            )
        );
    }

    public static FileMetadata[] MergeFileMetadata(FileMetadata[] updated, FileMetadata[] current)
    {
        var result = new List<FileMetadata>();

        foreach (var upd in updated)
        {
            var curr = current.FirstOrDefault(c => StringComparer.OrdinalIgnoreCase.Equals(c.Type, upd.Type));

            // If file is about to update add it as is
            if (upd.Source != null)
            {
                result.Add(upd);
                continue;
            }

            // Someone tries to fake and overwrite existing file
            if (curr == null)
                throw new InvalidOperationException($"Invalid file provided: file {upd.Type} has no source and not saved before");

            // If no reuploading requested then use current file
            result.Add(curr);
        }

        foreach (var curr in current)
        {
            var upd = updated.FirstOrDefault(u => StringComparer.OrdinalIgnoreCase.Equals(u.Type, curr.Type));

            // If no file were provided keep current file
            if (upd == null)
                result.Add(curr);
        }

        return result.ToArray();
    }
}