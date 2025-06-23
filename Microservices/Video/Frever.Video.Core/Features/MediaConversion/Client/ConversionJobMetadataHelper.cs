using System;
using System.Collections.Generic;
using Frever.Video.Core.Features.Shared;

#pragma warning disable CS8603, CS8618


namespace Frever.Video.Core.Features.MediaConversion.Client;

public static class ConversionJobMetadataHelper
{
    public const string JobMetadataVideoId = "VideoId";
    public const string JobMetadataConversionType = "ConversionType";
    public const string JobMetadataVersion = "V";

    public const byte Version = 2;

    public static Dictionary<string, string> CreateMetadata(long videoId, VideoConversionType conversionType)
    {
        var metadata = new Dictionary<string, string>
                       {
                           {JobMetadataVideoId, videoId.ToString()},
                           {JobMetadataConversionType, conversionType.ToString()},
                           {JobMetadataVersion, Version.ToString()}
                       };

        return metadata;
    }

    public static VideoMetadata ParseMetadata(Dictionary<string, string> metadata)
    {
        if (metadata == null || metadata.Count == 0)
            return null;

        var result = new VideoMetadata();

        if (metadata.TryGetValue(JobMetadataVideoId, out var videoIdStr) && long.TryParse(videoIdStr, out var videoId))
            result.Id = videoId;

        if (metadata.TryGetValue(JobMetadataConversionType, out var videoConversionType) &&
            Enum.TryParse<VideoConversionType>(videoConversionType, out var conversionType))
            result.ConversionType = conversionType;

        if (metadata.TryGetValue(JobMetadataVersion, out var versionStr) && byte.TryParse(versionStr, out var version))
            result.Version = version;

        return result;
    }
}

public class VideoMetadata
{
    public long Id { get; set; }

    public byte Version { get; set; } = ConversionJobMetadataHelper.Version;

    public VideoConversionType ConversionType { get; set; }
}