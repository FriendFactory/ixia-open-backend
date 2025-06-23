using Common.Models.Files;

namespace AssetServer.Models.AssetFileSettingsDTO;

public class FileSettingsDTO
{
    public FileType? File { get; set; }

    public Resolution? Resolution { get; set; }

    public FileExtension[] Extensions { get; set; }

    public bool IsPlatformDependent { get; set; }
}