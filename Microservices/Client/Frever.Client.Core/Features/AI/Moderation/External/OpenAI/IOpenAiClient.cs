using System.Threading.Tasks;
using Frever.Shared.MainDb.Entities;
using Newtonsoft.Json;

namespace Frever.Client.Core.Features.AI.Moderation.External.OpenAi;

public interface IOpenAiClient
{
    Task<OpenAiModerationResponse> Moderate(TextInput text, ImageUrlInput image);
}

public class OpenAiModerationRequest
{
    [JsonProperty("model")] public required string Model { get; set; }

    [JsonProperty("input")] public ModerationInputBase[] Input { get; set; }
}

public abstract class ModerationInputBase
{
    [JsonProperty("type")] public abstract string Type { get; }
}

public class TextInput : ModerationInputBase
{
    public override string Type => "text";

    [JsonProperty("text")] public required string Text { get; set; }
}

public class ImageUrlInput : ModerationInputBase
{
    public override string Type => "image_url";
    
    [JsonProperty("image_url")] public required ImageUrl ImageUrl { get; set; }
}

public class ImageUrl
{
    [JsonProperty("url")] public required string Url { get; set; }
}