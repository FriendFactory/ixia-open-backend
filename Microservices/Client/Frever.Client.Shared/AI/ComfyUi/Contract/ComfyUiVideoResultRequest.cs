using Common.Infrastructure.Messaging;
using Newtonsoft.Json;

namespace Frever.Client.Shared.AI.ComfyUi.Contract;

public class ComfyUiVideoResultRequest
{
    public string Env { get; set; }
    public string S3Bucket { get; set; }
    public string InputS3Key { get; set; }
    public long GroupId { get; set; }
    public string Workflow { get; set; }
    public string PartialName { get; set; }

    public string ToJson()
    {
        return JsonConvert.SerializeObject(this, SnsMessagingSettings.SerializerSettings);
    }
}