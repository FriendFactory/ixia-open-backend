using System;
using Common.Models.Files;

namespace AssetStoragePathProviding.Settings;

public abstract class FileSettings
{
    public readonly FileExtension[] Extensions;
    public readonly FileType FileType;
    public readonly bool IsPlatformDependent;
    public readonly string Name;
    public readonly Resolution? Resolution;

    protected FileSettings(
        FileType fileType,
        string name,
        FileExtension extension,
        bool isPlatformDependent,
        Resolution? resolution = null
    ) : this(
        fileType,
        name,
        [extension],
        isPlatformDependent,
        resolution
    ) { }

    protected FileSettings(
        FileType fileType,
        string name,
        FileExtension[] extensions,
        bool isPlatformDependent,
        Resolution? resolution = null
    )
    {
        FileType = fileType;
        Name = name;
        Extensions = extensions;
        IsPlatformDependent = isPlatformDependent;
        Resolution = resolution;
    }

    public override bool Equals(object obj)
    {
        if (obj == null || GetType() != obj.GetType())
            return false;

        var settings = (FileSettings) obj;

        return FileType == settings.FileType && Resolution == settings.Resolution;
    }

    protected bool Equals(FileSettings other)
    {
        return FileType == other.FileType && Resolution == other.Resolution;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine((int) FileType, Resolution);
    }

    public bool IsValidFor(FileInfo fileInfo)
    {
        return fileInfo.File == FileType && fileInfo.Resolution == Resolution;
    }

    public override string ToString()
    {
        return $"{FileType} {Resolution?.ToString()}";
    }
}