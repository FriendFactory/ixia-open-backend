using System;
using Common.Infrastructure.Messaging;
using Newtonsoft.Json;

namespace Frever.Client.Shared.AI.ComfyUi.Contract;

public class ComfyUiVideoLivePortraitMessage : IComfyUiMessage
{
    public string PartialName { get; set; } = Guid.NewGuid().ToString();
    public string Env { get; set; }
    public long GroupId { get; set; }
    public string S3Bucket { get; set; }
    public string InputS3Key { get; set; }
    public string SourceAudioS3Bucket { get; set; }
    public string SourceAudioS3Key { get; set; }
    public string SourceVideoS3Bucket { get; set; }
    public string SourceVideoS3Key { get; set; }
    public int SourceAudioStartTime { get; set; }
    public int SourceAudioDuration { get; set; }
    public int LivePortraitAudioInputModeContextValue { get; set; }
    public int LivePortraitCopperModeContextValue { get; set; }
    public int LivePortraitModelModeContextValue { get; set; }

    public void Enrich(string env, string s3Bucket, long groupId)
    {
        Env = env;
        GroupId = groupId;
        S3Bucket ??= s3Bucket;
        SourceVideoS3Bucket = SourceVideoS3Key != null && SourceVideoS3Bucket == null ? s3Bucket : S3Bucket;
        SourceAudioS3Bucket = SourceAudioS3Key != null && SourceAudioS3Bucket == null ? s3Bucket : S3Bucket;
        LivePortraitAudioInputModeContextValue = LivePortraitAudioInputModeContextValue == 0
                                                     ? (int) AudioInputMode.InputAudio
                                                     : LivePortraitAudioInputModeContextValue;
        LivePortraitCopperModeContextValue = LivePortraitCopperModeContextValue == 0
                                                 ? (int) CopperMode.IfCopperModeCuda
                                                 : LivePortraitCopperModeContextValue;
        LivePortraitModelModeContextValue = LivePortraitModelModeContextValue == 0
                                                ? (int) PortraitModelMode.Human
                                                : LivePortraitModelModeContextValue;
    }

    public void Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(S3Bucket);
        ArgumentException.ThrowIfNullOrWhiteSpace(InputS3Key);

        if (!Enum.IsDefined(typeof(AudioInputMode), LivePortraitAudioInputModeContextValue) ||
            !Enum.IsDefined(typeof(CopperMode), LivePortraitCopperModeContextValue) || !Enum.IsDefined(
                typeof(PortraitModelMode),
                LivePortraitModelModeContextValue
            ))
            throw new InvalidOperationException("Context values are invalid");

        switch (LivePortraitAudioInputModeContextValue)
        {
            case (int) AudioInputMode.InputAudio
                when string.IsNullOrWhiteSpace(SourceAudioS3Key) || string.IsNullOrWhiteSpace(SourceAudioS3Bucket):
                throw new InvalidOperationException("SourceAudioS3Bucket and SourceAudioS3Key are required for InputAudio mode");
            case (int) AudioInputMode.InputVideoDrivingAudio
                when string.IsNullOrWhiteSpace(SourceVideoS3Key) || string.IsNullOrWhiteSpace(SourceVideoS3Bucket):
                throw new InvalidOperationException(
                    "SourceVideoS3Key and SourceVideoS3Bucket are required for InputVideoDrivingAudio mode"
                );
        }
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