using System;
using System.Linq;
using Common.Models.Files;

namespace AssetStoragePathProviding.Settings;

internal sealed class AudioFileSettings : FileSettings
{
    private const string AudioFileName = "Audio";
    private static readonly FileExtension[] SupportedAudioExtensions = [FileExtension.Mp3, FileExtension.Ogg, FileExtension.Wav];

    public AudioFileSettings(FileExtension extension) : base(FileType.MainFile, AudioFileName, extension, false)
    {
        if (SupportedAudioExtensions.All(x => x != extension))
            throw new Exception($"Audio file can't have extension: {extension.ToString()}");
    }

    public AudioFileSettings(FileExtension[] extensions) : base(FileType.MainFile, AudioFileName, extensions, false)
    {
        if (extensions.Any(x => !SupportedAudioExtensions.Contains(x)))
            throw new Exception(
                $"Audio file can't have extension: {extensions.First(x => !SupportedAudioExtensions.Contains(x)).ToString()}"
            );
    }
}