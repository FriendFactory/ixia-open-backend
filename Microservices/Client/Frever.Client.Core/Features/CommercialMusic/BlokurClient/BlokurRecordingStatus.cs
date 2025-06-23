using Newtonsoft.Json;

namespace Frever.Client.Core.Features.CommercialMusic.BlokurClient;

public class BlokurRecordingStatus
{
    [JsonProperty("audio_provider_recording_id")] public string AudioProviderRecordingId { get; set; }

    [JsonProperty("title")] public string Title { get; set; }

    [JsonProperty("artists")] public string[] Artists { get; set; } = { };

    [JsonProperty("isrc")] public string Isrc { get; set; }

    [JsonProperty("version")] public string Version { get; set; }

    [JsonProperty("cleared")] public bool Cleared { get; set; }

    [JsonProperty("excluded_countries")] public string[] ExcludedCountries { get; set; }
}