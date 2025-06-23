using Newtonsoft.Json;

namespace Frever.Client.Core.Features.CommercialMusic.BlokurClient;

public class BlokurStatusTestRequest
{
    [JsonProperty("recordings")] public BlokurRecordingInput[] Recordings { get; set; }
}