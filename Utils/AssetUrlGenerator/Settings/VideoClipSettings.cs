using Common.Models.Files;

namespace AssetStoragePathProviding.Settings;

internal sealed class VideoClipSettings() : FileSettings(FileType.MainFile, "VideoClip", [FileExtension.Mp4], false);