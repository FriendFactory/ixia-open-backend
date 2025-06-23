using Newtonsoft.Json;

namespace Frever.Client.Core.Features.CommercialMusic.BlokurClient;

public class BlokurRecordingInput
{
    [JsonProperty("audio_provider_recording_id")] public string AudioProviderRecordingId { get; set; }

    [JsonProperty("title")] public string Title { get; set; }

    [JsonProperty("artists")] public string[] Artists { get; set; } = { };

    [JsonProperty("isrc")] public string Isrc { get; set; }
}