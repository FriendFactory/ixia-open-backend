using System.Collections.ObjectModel;
using System.Linq;
using Common.Models.Files;

namespace AssetStoragePathProviding.Settings;

public class AssetFilesSetting(FileSettings[] fileSettings)
{
    public ReadOnlyCollection<FileSettings> FilesSettings { get; } = new(fileSettings);

    public bool IsPlatformDependent
    {
        get
        {
            var mainFileSettings = GetSettings(FileType.MainFile);

            return mainFileSettings != null && mainFileSettings.IsPlatformDependent;
        }
    }

    public FileSettings GetSettings(FileType fileType, Resolution? resolution = null)
    {
        return FilesSettings.FirstOrDefault(x => x.FileType == fileType && x.Resolution == resolution);
    }
}