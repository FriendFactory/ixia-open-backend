using System;
using Common.Infrastructure.Messaging;
using Newtonsoft.Json;

namespace Frever.Client.Shared.AI.ComfyUi.Contract;

public class ComfyUiLatentSyncMessage : IComfyUiMessage
{
    public string PartialName { get; set; } = Guid.NewGuid().ToString();
    public string Env { get; set; }
    public string S3Bucket { get; set; }
    public string VideoS3Key { get; set; }
    public string AudioS3Key { get; set; }
    public string AudioS3Bucket { get; set; }
    public long GroupId { get; set; }
    public long LevelId { get; set; }
    public long VideoId { get; set; }
    public string Version { get; set; }
    public int VideoDurationSeconds { get; set; }
    public int StartTimeSeconds { get; set; }
    public int ResultVideoDurationSeconds { get; set; }

    public void Enrich(string env, string s3Bucket, long groupId)
    {
        Env = env;
        GroupId = groupId;
        S3Bucket = string.IsNullOrWhiteSpace(S3Bucket) ? s3Bucket : S3Bucket;
        AudioS3Bucket = s3Bucket;
        VideoId = -1;
        LevelId = -1;
        Version = string.Empty;
        VideoDurationSeconds = 10;
        StartTimeSeconds = 2;
        ResultVideoDurationSeconds = 5;
    }

    public void Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(S3Bucket);
        ArgumentException.ThrowIfNullOrWhiteSpace(VideoS3Key);
        ArgumentException.ThrowIfNullOrWhiteSpace(AudioS3Key);
        ArgumentException.ThrowIfNullOrWhiteSpace(AudioS3Bucket);
    }

    public string ToResultKey(string workflow)
    {
        return ComfyUiClient.ToResultKey(VideoS3Key, S3Bucket, workflow, PartialName);
    }

    public string ToJson()
    {
        return JsonConvert.SerializeObject(this, SnsMessagingSettings.SerializerSettings);
    }
}