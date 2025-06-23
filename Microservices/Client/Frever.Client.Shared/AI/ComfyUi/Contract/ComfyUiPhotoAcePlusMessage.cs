using System;
using Common.Infrastructure.Messaging;
using Newtonsoft.Json;

namespace Frever.Client.Shared.AI.ComfyUi.Contract;

public class ComfyUiPhotoAcePlusMessage : IComfyUiMessage
{
    public string PartialName { get; set; } = Guid.NewGuid().ToString();
    public string Env { get; set; }
    public string S3Bucket { get; set; }
    public string SourceS3Bucket { get; set; }
    public string MaskS3Bucket { get; set; }
    public string InputS3Key { get; set; }
    public string SourceS3Key { get; set; }
    public string MaskS3Key { get; set; }
    public long GroupId { get; set; }
    public string PromptText { get; set; }
    public int AcePlusWardrobeModeContextValue { get; set; }
    public int AcePlusReferenceModeContextValue { get; set; }
    public int AcePlusMaskModeContextValue { get; set; }

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

        if (!Enum.IsDefined(typeof(WardrobeMode), AcePlusWardrobeModeContextValue) ||
            !Enum.IsDefined(typeof(ReferenceMode), AcePlusReferenceModeContextValue) || !Enum.IsDefined(
                typeof(MaskMode),
                AcePlusMaskModeContextValue
            ))
            throw new InvalidOperationException("Context values are invalid");

        if (AcePlusReferenceModeContextValue == (int) ReferenceMode.UploadImage &&
            (string.IsNullOrWhiteSpace(SourceS3Key) || string.IsNullOrWhiteSpace(SourceS3Bucket)))
            throw new InvalidOperationException("SourceS3Bucket and SourceS3Key are required for UploadImage mode");

        if (AcePlusMaskModeContextValue == (int) MaskMode.Manual &&
            (string.IsNullOrWhiteSpace(MaskS3Key) || string.IsNullOrWhiteSpace(MaskS3Bucket)))
            throw new InvalidOperationException("MaskS3Bucket and MaskS3Key are required for ManualMask mode");
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