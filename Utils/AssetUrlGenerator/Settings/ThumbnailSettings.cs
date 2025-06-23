using Common.Models.Files;

namespace AssetStoragePathProviding.Settings;

internal sealed class ThumbnailSettings(FileExtension extension, Resolution resolution) : ImageSettings(
    FileType.Thumbnail,
    [extension],
    resolution,
    FileType.Thumbnail.ToString()
);