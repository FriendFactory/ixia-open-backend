using Newtonsoft.Json;

namespace Frever.Client.Shared.AI.ComfyUi.Contract;

public class ComfyUiResultResponse
{
    public string Workflow { get; set; }
    [JsonProperty("bucket")] public string S3Bucket { get; set; }
    [JsonProperty("key")] public string S3Key { get; set; }
    [JsonProperty("mainKey")] public string MainKey { get; set; }
    [JsonProperty("coverKey")] public string CoverKey { get; set; }
    [JsonProperty("thumbnailKey")] public string ThumbnailKey { get; set; }
    [JsonProperty("maskKey")] public string MaskKey { get; set; }
}