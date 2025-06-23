using System.Collections.Generic;

namespace AssetServer.Models.AssetFileSettingsDTO;

public class AssetFilesSettings
{
    public string AssetTypeName { get; set; }
    public List<FileSettingsDTO> Settings { get; set; }
}