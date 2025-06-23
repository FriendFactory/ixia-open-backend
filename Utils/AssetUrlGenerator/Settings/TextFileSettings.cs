using Common.Models.Files;

namespace AssetStoragePathProviding.Settings;

internal sealed class TextFileSettings(string name) : FileSettings(FileType.MainFile, name, FileExtension.Txt, false);