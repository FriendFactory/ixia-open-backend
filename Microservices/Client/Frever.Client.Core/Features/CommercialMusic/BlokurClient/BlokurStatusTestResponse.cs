using Newtonsoft.Json;

namespace Frever.Client.Core.Features.CommercialMusic.BlokurClient;

public class BlokurStatusTestResponse
{
    public bool Ok { get; set; }
    [JsonProperty("recordings")] public BlokurRecordingStatus[] Recordings { get; set; }
}