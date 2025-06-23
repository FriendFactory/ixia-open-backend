using Newtonsoft.Json;

namespace Frever.Client.Core.Features.CommercialMusic.BlokurClient;

public class BlokurClearedTrackResponse
{
    [JsonIgnore] public bool Ok { get; set; }

    [JsonProperty("file_url")] public string ClearedTracksCsvUrl { get; set; }
}