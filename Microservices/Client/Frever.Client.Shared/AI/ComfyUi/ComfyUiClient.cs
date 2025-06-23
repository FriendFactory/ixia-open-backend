using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Amazon.SQS;
using Common.Infrastructure.EnvironmentInfo;
using Common.Infrastructure.Messaging;
using Common.Infrastructure.ServiceDiscovery;
using Frever.Client.Shared.AI.ComfyUi.Contract;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Frever.Client.Shared.AI.ComfyUi;

public interface IComfyUiClient
{
    Task<ComfyUiResponse> PostGeneration(string workflow, IComfyUiMessage message);
    Task<ComfyUiResultResponse> GetResult(string resultKey, long groupId);
}

public sealed class ComfyUiClient(
    IAmazonSQS sqs,
    ILogger<ComfyUiClient> log,
    ComfyUiApiSettings settings,
    EnvironmentInfo environmentInfo,
    ServiceUrls serviceUrls
) : IComfyUiClient
{
    public static readonly List<string> AllowedFormats =
    [
        "jpg",
        "jpeg",
        "webp",
        "png",
        "mp3",
        "mp4"
    ];

    public async Task<ComfyUiResponse> PostGeneration(string workflow, IComfyUiMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentException.ThrowIfNullOrWhiteSpace(workflow);

        if (!Workflows.TryGetSubject(workflow, out var subject))
            return ComfyUiResponse.Failed($"No subject specified for workflow {workflow}");

        var snsMessage = new SnsMessage {Subject = subject, Payload = message.ToJson(), MessageId = Guid.NewGuid().ToString()};
        var jsonMessage = snsMessage.ToJson();
        try
        {
            await sqs.SendMessageAsync(settings.QueueUrl, jsonMessage);
            return ComfyUiResponse.Success;
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Image generation exception");
            return ComfyUiResponse.Failed("Error starting image generation");
        }
        finally
        {
            log.LogInformation("Send {Message} to  SQS", jsonMessage);
        }
    }

    public static string ToResultKey(string s3Key, string s3Bucket, string workflow, string partialName = null)
    {
        return $"{workflow}|{partialName}|{s3Bucket}|{s3Key}";
    }

    public static (string Workflow, string PartialName, string S3Bucket, string S3Key) FromResultKey(string key)
    {
        var strings = key.Split('|');
        return (strings[0], strings[1], strings[2], strings[3]);
    }

    public async Task<ComfyUiResultResponse> GetResult(string resultKey, long groupId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resultKey);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(groupId);

        var data = FromResultKey(resultKey);
        if (!Workflows.AllKeys.Contains(data.Workflow))
            return null;

        var url = GetUrl(data.Workflow);

        var content = GetContent(groupId, data);

        var uriBuilder = new UriBuilder(new Uri(new Uri(serviceUrls.MachineLearning, UriKind.Absolute), new Uri(url, UriKind.Relative)));

        using var handler = new HttpClientHandler();
        handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

        using var client = new HttpClient(handler);
        client.Timeout = TimeSpan.FromSeconds(5);

        using var request = new HttpRequestMessage(HttpMethod.Post, uriBuilder.Uri);
        request.Content = new StringContent(content, Encoding.UTF8, "application/json");

        log.LogInformation("Request content: {Content}", content);

        using var response = await client.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            log.LogWarning("ComfyUiResult failed: {StatusCode} {ReasonPhrase}", response.StatusCode, response.ReasonPhrase);
            return null;
        }

        var body = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<ComfyUiResultResponse>(body);
        result.Workflow = data.Workflow;
        return result;
    }

    private string GetContent(long groupId, (string Workflow, string PartialName, string S3Bucket, string S3Key) data)
    {
        var request = new ComfyUiResultRequest
                      {
                          Env = environmentInfo.Type,
                          GroupId = groupId,
                          PartialName = data.PartialName,
                          InputS3Key = string.IsNullOrWhiteSpace(data.S3Key) ? null : data.S3Key,
                          S3Bucket = string.IsNullOrWhiteSpace(data.S3Bucket) ? null : data.S3Bucket
                      };

        if (Workflows.PhotoWorkflows.Contains(data.Workflow))
            request.PhotoWorkflow = data.Workflow;
        else
            request.Workflow = data.Workflow;

        return request.ToJson();
    }

    private static string GetUrl(string workflow)
    {
        if (Workflows.VideoWorkflows.Contains(workflow))
            return "api/comfyui/comfyui-result";
        return Workflows.ImageOutputWorkflows.Contains(workflow) ? "api/comfyui/photo-multi-result" : "api/comfyui/photo-result";
    }
}

public enum MusicGenContext
{
    NarrationInput = 1,
    MixIncomingAudio = 2,
    MuteIncomingAudio = 3
}

public enum AudioPromptMode
{
    AutoPrompt = 1,
    PromptOnly = 2,
    AutoPromptPlusPromptAppend = 3
}

public enum AudioAudioMode
{
    MuteIncomingAudioBackgroundMusicNoVoices = 1,
    MuteIncomingAudioBackgroundMusicWithVoices = 2,
    NarrationInputBackgroundMusicNoVoices = 3,
    MixIncomingAudioBackgroundAudioWithSfxNoVoices = 4,
    NarrationInputBackgroundMusicWithVoices = 5,
    MixIncomingAudioBackgroundAudioWithSfxWithVoices = 6,
    MixIncomingAudioBackgroundAudioWithSfxNoVoicesLowerVolume = 7,
    MixIncomingAudioBackgroundAudioWithSfxWithVoicesLowerVolume = 8
}

public enum ReferenceMode
{
    UploadImage = 1,
    Prompt = 2
}

public enum MaskMode
{
    Auto = 1,
    Manual = 2
}

public enum WardrobeMode
{
    FullClothes = 1,
    Hair = 2,
    Hats = 3,
    Glasses = 4,
    Bags = 5,
    Necklace = 6,
    Shoes = 7,
    FullClothesAndHairNoFace = 8
}

public enum AudioInputMode
{
    InputAudio = 1,
    InputVideoDrivingAudio = 2,
    AudioEmbeddedInTargetVideo = 3
}

public enum CopperMode
{
    IfCopperModeCuda = 1,
    MpCopperModeCuda = 2
}

public enum PortraitModelMode
{
    Human = 1,
    Animal = 2
}