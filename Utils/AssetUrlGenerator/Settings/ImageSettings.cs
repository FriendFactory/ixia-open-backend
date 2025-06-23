using System;
using System.Linq;
using Common.Models.Files;

namespace AssetStoragePathProviding.Settings;

internal class ImageSettings : FileSettings
{
    private static readonly FileExtension[] AllowedExtensions =
    [
        FileExtension.Png, FileExtension.Gif, FileExtension.Jpg, FileExtension.Jpeg, FileExtension.Mp4
    ];

    public ImageSettings(FileType fileType, FileExtension[] extensions, Resolution? resolution, string fileName) : base(
        fileType,
        fileName + resolution,
        extensions,
        false,
        resolution
    )
    {
        foreach (var extension in extensions)
            ValidateExtensions(extension);
    }

    private void ValidateExtensions(FileExtension extension)
    {
        if (AllowedExtensions.All(x => x != extension))
            throw new Exception($"{FileType.ToString()} can't have {extension.ToString()} extension.");
    }
}