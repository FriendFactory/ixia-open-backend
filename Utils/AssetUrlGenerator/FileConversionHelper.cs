using System;
using System.IO;
using System.Linq;

namespace AssetStoragePathProviding;

public static class FileConversionHelper
{
    public const string ConversionSuffix = ".convert";

    private static readonly string[] ImageExtensions = {".jpg", ".jpeg", ".png", ".heic", ".gif"};

    private static readonly string[] VideoExtensions = {".mov", ".avi", ".mp4"};

    public static ConversionInfo GetConversionInfoFromExtension(string originalFileExtension)
    {
        var failedConversion = new ConversionInfo
                               {
                                   CanConvert = false,
                                   MediaType = MediaType.Unsupported,
                                   NeedsConversion = false,
                                   OriginalFileExtension = originalFileExtension
                               };

        if (string.IsNullOrWhiteSpace(originalFileExtension))
            return failedConversion;

        originalFileExtension = $".{originalFileExtension.Trim('.')}";

        if (ImageExtensions.Contains(originalFileExtension, StringComparer.InvariantCultureIgnoreCase))
            return new ConversionInfo
                   {
                       CanConvert = true,
                       NeedsConversion =
                           !StringComparer.OrdinalIgnoreCase.Equals(originalFileExtension, ".jpg") &&
                           !StringComparer.OrdinalIgnoreCase.Equals(originalFileExtension, ".jpeg"),
                       MediaType = MediaType.Image,
                       OriginalFileExtension = originalFileExtension,
                       TargetFileExtension = ".jpg"
                   };

        if (VideoExtensions.Contains(originalFileExtension, StringComparer.InvariantCultureIgnoreCase))
            return new ConversionInfo
                   {
                       CanConvert = true,
                       NeedsConversion = true,
                       MediaType = MediaType.Video,
                       OriginalFileExtension = originalFileExtension,
                       TargetFileExtension = ".mp4"
                   };

        return failedConversion;
    }

    public static bool IsMarkedForConversion(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(path));

        return path.EndsWith(ConversionSuffix, StringComparison.InvariantCultureIgnoreCase);
    }

    public static ConversionInfo GetConversionInfoFromPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(path));

        path = GetOriginalFilePath(path);

        var extension = Path.GetExtension(path);

        return GetConversionInfoFromExtension(extension);
    }

    public static string GetOriginalFilePath(string sourceFilePath)
    {
        if (string.IsNullOrWhiteSpace(sourceFilePath))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(sourceFilePath));

        if (IsMarkedForConversion(sourceFilePath))
            sourceFilePath = sourceFilePath.Replace(ConversionSuffix, string.Empty, StringComparison.InvariantCultureIgnoreCase);

        return sourceFilePath;
    }


    public static string GetTargetFilePath(string sourceFilePath)
    {
        if (string.IsNullOrWhiteSpace(sourceFilePath))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(sourceFilePath));

        var originalFilePath = GetOriginalFilePath(sourceFilePath);

        var conversionInfo = GetConversionInfoFromPath(originalFilePath);

        if (conversionInfo.MediaType == MediaType.Unsupported)
            throw new ArgumentException("File can't be converted");

        return Path.ChangeExtension(originalFilePath, conversionInfo.TargetFileExtension);
    }
}

public class ConversionInfo
{
    public MediaType MediaType { get; set; }

    public string OriginalFileExtension { get; set; }

    public string TargetFileExtension { get; set; }

    public bool NeedsConversion { get; set; }

    public bool CanConvert { get; set; }
}

public enum MediaType
{
    Image,
    Video,
    Unsupported
}