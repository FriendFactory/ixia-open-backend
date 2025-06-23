using System;
using System.Collections.Generic;
using Common.Infrastructure.Messaging;
using Newtonsoft.Json;

namespace Frever.Client.Shared.AI.ComfyUi.Contract;

public class ComfyUiMultiPhotoAndPromptMessage : IComfyUiMessage
{
    public string PartialName { get; set; } = Guid.NewGuid().ToString();
    public string Env { get; set; }
    public string S3Bucket { get; set; }
    public string InputS3Key { get; set; }
    public string SourceS3Bucket { get; set; }
    public List<string> SourceS3Keys { get; set; }
    public string PromptText { get; set; }
    public long GroupId { get; set; }

    public void Enrich(string env, string s3Bucket, long groupId)
    {
        Env = env;
        GroupId = groupId;
        S3Bucket = string.IsNullOrWhiteSpace(S3Bucket) ? s3Bucket : S3Bucket;
        SourceS3Bucket = string.IsNullOrWhiteSpace(SourceS3Bucket) ? s3Bucket : SourceS3Bucket;
    }

    public void Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(S3Bucket);
        ArgumentException.ThrowIfNullOrWhiteSpace(InputS3Key);

        if(string.IsNullOrWhiteSpace(SourceS3Bucket) && string.IsNullOrWhiteSpace(PromptText))
            throw new ArgumentException("AudioS3Bucket or PromptText required");

        if(!string.IsNullOrWhiteSpace(SourceS3Bucket) && (SourceS3Keys == null || SourceS3Keys.Count == 0))
            throw new ArgumentException("SourceS3Bucket and SourceS3Keys required");
    }

    public string ToResultKey(string workflow)
    {
        return ComfyUiClient.ToResultKey(InputS3Key, S3Bucket, workflow, PartialName);
    }

    public string ToJson()
    {
        return JsonConvert.SerializeObject(this, SnsMessagingSettings.SerializerSettings);
    }
}