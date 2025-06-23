using System;

namespace AssetStoragePathProviding;

public class VideoNamingHelper(VideoNamingHelperOptions config)
{
    private readonly VideoNamingHelperOptions _config = config ?? throw new ArgumentNullException(nameof(config));

    public string VideoBucket => _config.DestinationVideoBucket;
    public string SourceVideoBucket => _config.IngestVideoBucket;

    public string GetUploadVideoS3Key(long groupId, string uploadId)
    {
        if (string.IsNullOrWhiteSpace(uploadId))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(uploadId));

        return $"{GetUploadVideoS3KeyPrefix(uploadId)}" + $"_Group:{groupId}" + $"_.mp4";
    }

    public string GetUploadVideoS3Path(long groupId, string uploadId)
    {
        if (string.IsNullOrWhiteSpace(uploadId))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(uploadId));

        return $"s3://{_config.IngestVideoBucket}/{GetUploadVideoS3Key(groupId, uploadId)}";
    }

    public string GetUploadVideoS3KeyPrefix(string uploadId)
    {
        return $"temp-uploads/T{uploadId ?? throw new ArgumentNullException(nameof(uploadId))}";
    }

    public string GetSourceVideoS3Key(long groupId, long levelId)
    {
        return $"Video/{groupId}/{levelId}.mp4";
    }

    public string GetSourceVideoS3Path(long groupId, long levelId)
    {
        return $"s3://{_config.IngestVideoBucket}/{GetSourceVideoS3Key(groupId, levelId)}";
    }

    public string GetVideoS3Path(IVideoNameSource video)
    {
        return $"s3://{_config.DestinationVideoBucket}/{GetVideoFolder(video)}";
    }

    public string GetDestS3Path(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        key = key.TrimStart('/');
        return $"s3://{_config.DestinationVideoBucket}/{key}";
    }

    public string GetVideoUrl(IVideoNameSource video)
    {
        return $"{GetBaseVideoUrl(video)}/video.m3u8";
    }

    public string GetSharingVideoUrl(IVideoNameSource video)
    {
        return $"{GetBaseVideoUrl(video)}/video_raw.mp4";
    }

    public string GetVideoThumbnailUrl(IVideoNameSource video)
    {
        return $"{GetBaseVideoUrl(video)}/video_thumbnail.mp4";
    }

    public string GetSignedCookieResourcePath(IVideoNameSource video)
    {
        return $"{GetBaseVideoUrl(video)}/*";
    }

    public string GetRawVideoPath(IVideoNameSource video)
    {
        return $"{GetVideoFolder(video)}/video_raw.mp4";
    }

    public string GetVideoFolder(IVideoNameSource video)
    {
        ArgumentNullException.ThrowIfNull(video);

        return $"Video/{video.GroupId}/NonLevel/{video.Id}" +
               (string.IsNullOrWhiteSpace(video.Version) ? string.Empty : $"/{video.Version}");
    }

    public string GetVideoMainFolderPathByGroupId(long groupId)
    {
        return $"Video/{groupId}";
    }

    private string GetBaseVideoUrl(IVideoNameSource video)
    {
        ArgumentNullException.ThrowIfNull(video);

        return $"{_config.CloudFrontHost.TrimEnd('/')}/{GetVideoFolder(video)}";
    }
}