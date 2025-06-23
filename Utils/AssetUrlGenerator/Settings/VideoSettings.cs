using Common.Models.Files;

namespace AssetStoragePathProviding.Settings;

internal sealed class VideoSettings() : FileSettings(FileType.MainFile, "Video", [FileExtension.Mp4, FileExtension.Mov], false);