using System;
using Common.Infrastructure.Messaging;
using Newtonsoft.Json;

namespace Frever.Client.Shared.AI.ComfyUi.Contract;

public class ComfyUiPhotoAndPromptMessage : IComfyUiMessage
{
    public string PartialName { get; set; } = Guid.NewGuid().ToString();
    public string Env { get; set; }
    public string S3Bucket { get; set; }
    public string InputS3Key { get; set; }
    public string PromptText { get; set; }
    public long GroupId { get; set; }

    public void Enrich(string env, string s3Bucket, long groupId)
    {
        Env = env;
        GroupId = groupId;
    }

    public void Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(PromptText);
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