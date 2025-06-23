using Common.Models.Files;

namespace AssetStoragePathProviding.Settings;

internal sealed class AssetBundleSettings() : FileSettings(FileType.MainFile, "AssetBundle", [FileExtension.Empty], true);